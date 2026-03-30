using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoBank.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- Accounts ---

    [Fact]
    public async Task GetAccounts_Returns200WithSeededData()
    {
        var response = await _client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<Account>>(JsonOptions);
        Assert.NotNull(accounts);
        Assert.True(accounts.Count >= 5, $"Expected at least 5 seeded accounts, got {accounts.Count}");
    }

    [Fact]
    public async Task GetAccountById_Returns200_WhenExists()
    {
        var response = await _client.GetAsync("/api/accounts/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<Account>(JsonOptions);
        Assert.NotNull(account);
        Assert.Equal(1, account.Id);
    }

    [Fact]
    public async Task GetAccountById_Returns404ProblemDetails_WhenNotExists()
    {
        var response = await _client.GetAsync("/api/accounts/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Account not found", problem.Title);
    }

    // --- Transfers ---

    [Fact]
    public async Task GetTransfers_Returns200()
    {
        var response = await _client.GetAsync("/api/transfers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostTransfer_Returns201_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 100m);
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await response.Content.ReadFromJsonAsync<Transfer>(JsonOptions);
        Assert.NotNull(transfer);
        Assert.Equal(100m, transfer.Amount);
    }

    [Fact]
    public async Task PostTransfer_Returns400ProblemDetails_SameAccount()
    {
        var request = new TransferRequest(1, 1, 100m);
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task PostTransfer_Returns400_InsufficientFunds()
    {
        var request = new TransferRequest(1, 2, 999_999_999m);
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("Insufficient funds", problem.Detail);
    }

    [Fact]
    public async Task PostWireTransfer_Returns201_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 50m);
        var response = await _client.PostAsJsonAsync("/api/transfers/wire", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostInternationalTransfer_Returns201_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 75m);
        var response = await _client.PostAsJsonAsync("/api/transfers/international", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- Transactions ---

    [Fact]
    public async Task GetTransactions_Returns200WithSeededData()
    {
        var response = await _client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(JsonOptions);
        Assert.NotNull(transactions);
        Assert.True(transactions.Count >= 100);
    }

    [Fact]
    public async Task GetTransactions_WithAccountIdFilter_Returns200()
    {
        var response = await _client.GetAsync("/api/transactions?accountId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(JsonOptions);
        Assert.NotNull(transactions);
        Assert.All(transactions, t => Assert.Equal(1, t.AccountId));
    }

    [Fact]
    public async Task GetTransactions_WithCategoryFilter_Returns200()
    {
        var response = await _client.GetAsync("/api/transactions?category=Groceries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(JsonOptions);
        Assert.NotNull(transactions);
        Assert.All(transactions, t => Assert.Equal("Groceries", t.Category));
    }

    [Fact]
    public async Task GetTransactionById_Returns404_WhenNotExists()
    {
        var response = await _client.GetAsync("/api/transactions/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Reports ---

    [Fact]
    public async Task PostAnnualStatement_Returns200_WhenAccountExists()
    {
        var response = await _client.PostAsync("/api/reports/annual-statement?accountId=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("accountId", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostAnnualStatement_Returns404_WhenAccountNotExists()
    {
        var response = await _client.PostAsync("/api/reports/annual-statement?accountId=999", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReconciliation_Returns200()
    {
        var response = await _client.PostAsync("/api/reports/reconciliation", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalAccounts", body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Settings ---

    [Fact]
    public async Task PostVerifyIdentity_Returns200()
    {
        var response = await _client.PostAsync("/api/settings/verify-identity", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_Returns200()
    {
        var response = await _client.GetAsync("/api/settings/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Alex Johnson", body);
    }

    // --- Health Checks ---

    [Fact]
    public async Task LivenessEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
