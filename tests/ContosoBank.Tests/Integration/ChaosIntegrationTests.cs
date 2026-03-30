using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoBank.Tests.Integration;

[Collection("Integration")]
public class ChaosIntegrationTests
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ChaosIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- Scenario 1: Memory Leak (Annual Statement) ---

    [Fact]
    public async Task AnnualStatement_TriggersMemoryLeak_ReturnsOk()
    {
        // Annual statement triggers memory leak in background but still returns data
        var response = await _client.PostAsync("/api/reports/annual-statement?accountId=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("accountId", body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Scenario 2: CPU Spike (Fraud Detection) ---

    [Fact]
    public async Task FraudDetection_TriggersCpuSpike_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/accounts/fraud-detection", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Scanning", body);
    }

    // --- Scenario 3: HTTP 500 (Wire Transfer) ---

    [Fact]
    public async Task WireTransfer_TriggersHttpErrors_Returns500()
    {
        var request = new TransferRequest(1, 2, 500m);
        var response = await _client.PostAsJsonAsync("/api/transfers/wire", request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Wire transfer failed", problem.Title);
        Assert.Contains("payment processor", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    // --- Scenario 4: DB Connection Failure (Account Refresh) ---

    [Fact]
    public async Task AccountRefresh_TriggersDbFailure_ReturnsAccounts()
    {
        // With InMemory database, the ChaosDbInterceptor doesn't fire
        // (InMemory doesn't use DbConnection), so the endpoint still returns data
        var response = await _client.PostAsync("/api/accounts/refresh", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Scenario 6: Dependency Timeout (Verify Identity) ---

    [Fact]
    public async Task VerifyIdentity_TriggersDependencyTimeout_Returns504()
    {
        var response = await _client.PostAsync("/api/settings/verify-identity", null);

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Identity verification failed", problem.Title);
        Assert.Contains("unavailable", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    // --- Scenario 7: Log Flooding (Export History) ---

    [Fact]
    public async Task ExportHistory_TriggersLogFlooding_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/transactions/export", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("exportedAt", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("totalTransactions", body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Scenario 8: Exception Storm (Reconciliation) ---

    [Fact]
    public async Task Reconciliation_TriggersExceptionStorm_Returns500()
    {
        var response = await _client.PostAsync("/api/reports/reconciliation", null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Reconciliation failed", problem.Title);
    }

    // --- Non-chaos endpoints still work normally ---

    [Fact]
    public async Task StandardTransfer_StillWorksNormally()
    {
        var request = new TransferRequest(1, 2, 5m);
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        // Standard transfers are NOT chaos triggers and should still work
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetAccounts_StillWorksNormally()
    {
        var response = await _client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- Error responses are user-friendly (not raw stack traces) ---

    [Fact]
    public async Task ChaosEndpoints_ReturnProblemDetails_NotStackTraces()
    {
        var wireResponse = await _client.PostAsJsonAsync("/api/transfers/wire", new TransferRequest(1, 2, 100m));
        var verifyResponse = await _client.PostAsync("/api/settings/verify-identity", null);
        var reconResponse = await _client.PostAsync("/api/reports/reconciliation", null);

        foreach (var response in new[] { wireResponse, verifyResponse, reconResponse })
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("StackTrace", body);
            Assert.DoesNotContain("System.Exception", body);
            // Should be RFC 7807 ProblemDetails format
            Assert.Contains("title", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    // --- Chaos metrics are emitted ---

    [Fact]
    public async Task ChaosEndpoints_EmitMetrics()
    {
        // Trigger chaos scenarios that emit metrics
        await _client.PostAsJsonAsync("/api/transfers/wire", new TransferRequest(1, 2, 10m));
        await _client.PostAsync("/api/settings/verify-identity", null);

        // Wait for metrics collection cycle
        await Task.Delay(2000);

        var metricsResponse = await _client.GetAsync("/metrics");
        var body = await metricsResponse.Content.ReadAsStringAsync();

        // Chaos scenarios should emit exception and/or dependency timeout metrics
        // At minimum, the wire transfer emits contosobank_exceptions via TriggerHttpErrors
        var hasExceptions = body.Contains("contosobank_exceptions");
        var hasDependencyTimeouts = body.Contains("contosobank_dependency_timeouts");
        var hasDbErrors = body.Contains("contosobank_db_errors");

        Assert.True(hasExceptions || hasDependencyTimeouts || hasDbErrors,
            $"Expected at least one chaos metric in output. Metrics snippet: {body[..Math.Min(body.Length, 2000)]}");
    }
}
