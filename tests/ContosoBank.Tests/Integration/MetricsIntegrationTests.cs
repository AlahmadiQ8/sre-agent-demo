using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoBank.Tests.Integration;

[Collection("Integration")]
public class MetricsIntegrationTests
{
    private readonly HttpClient _client;

    public MetricsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MetricsEndpoint_Returns200_WithPrometheusFormat()
    {
        var response = await _client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);

        // Verify Prometheus text format contains metadata lines
        Assert.Contains("# HELP", body);
        Assert.Contains("# TYPE", body);
    }

    [Fact]
    public async Task MetricsEndpoint_ContainsCustomBankMetrics_AfterApiCalls()
    {
        // Make a successful transfer
        var successRequest = new TransferRequest(1, 2, 10m);
        var successResponse = await _client.PostAsJsonAsync("/api/transfers", successRequest);
        Assert.Equal(HttpStatusCode.Created, successResponse.StatusCode);

        // Make a failed transfer (same account)
        var failRequest = new TransferRequest(1, 1, 10m);
        await _client.PostAsJsonAsync("/api/transfers", failRequest);

        // Trigger reconciliation to generate TransactionsProcessed metrics
        await _client.PostAsync("/api/reports/reconciliation", null);

        // Wait for metrics collection cycle to capture all observations
        await Task.Delay(1000);

        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Assert transfer metrics with success label
        Assert.True(body.Contains("contosobank_transfers"),
            $"Expected 'contosobank_transfers' in metrics output:\n{body[..Math.Min(body.Length, 3000)]}");
        Assert.Matches(@"status\s*=\s*""success""", body);

        // Assert transfer metrics with failed label
        Assert.Matches(@"status\s*=\s*""failed""", body);

        // Assert transactions processed metrics
        Assert.Contains("contosobank_transactions_processed", body);
        Assert.Matches(@"type\s*=\s*""credit""", body);
        Assert.Matches(@"type\s*=\s*""debit""", body);
    }

    [Fact]
    public async Task MetricsEndpoint_PrometheusTextFormatIsValid()
    {
        // Generate some traffic first
        await _client.GetAsync("/api/accounts");
        await _client.PostAsJsonAsync("/api/transfers", new TransferRequest(1, 2, 1m));

        await Task.Delay(1000);

        var response = await _client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        var lines = body.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Each line is either a comment (#) or a metric sample (name{labels} value [timestamp])
            var isComment = line.StartsWith('#');
            var isMetricSample = Regex.IsMatch(line, @"^[a-zA-Z_:][a-zA-Z0-9_:]*(\{.*\})?\s+[\d.eE+\-]+");
            var isEof = line.Trim() == "# EOF";

            Assert.True(isComment || isMetricSample || isEof,
                $"Invalid Prometheus format line: {line}");
        }
    }
}
