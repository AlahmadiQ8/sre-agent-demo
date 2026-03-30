using System.Diagnostics.Metrics;
using ContosoBank.Data;
using ContosoBank.Metrics;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BankDbContext(options);
        _db.Database.EnsureCreated();

        _service = new ReportService(_db, Mock.Of<ILogger<ReportService>>(),
            new BankMetrics(new TestMeterFactory()));
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }

    private async Task<Account> SeedAccountWithTransactions()
    {
        var account = new Account { AccountNumber = "CHK-001", AccountName = "Test Account", Balance = 5000m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.Transactions.AddRange(
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Credit, Amount = 3000m,
                Description = "Salary", Category = "Payroll",
                Timestamp = DateTime.UtcNow.AddDays(-30)
            },
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Debit, Amount = 150m,
                Description = "Groceries", Category = "Groceries",
                Timestamp = DateTime.UtcNow.AddDays(-10)
            },
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Debit, Amount = 50m,
                Description = "Coffee", Category = "Dining",
                Timestamp = DateTime.UtcNow.AddDays(-1)
            });
        await _db.SaveChangesAsync();

        return account;
    }

    [Fact]
    public async Task GenerateAnnualStatementAsync_ReturnsStatement()
    {
        var account = await SeedAccountWithTransactions();

        var result = await _service.GenerateAnnualStatementAsync(account.Id);

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal("CHK-001", result.AccountNumber);
        Assert.Equal("Test Account", result.AccountName);
        Assert.Equal(5000m, result.Balance);
        Assert.Equal(3, result.Transactions.Count());
    }

    [Fact]
    public async Task GenerateAnnualStatementAsync_ReturnsNull_WhenAccountNotFound()
    {
        var result = await _service.GenerateAnnualStatementAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAnnualStatementAsync_TransactionsAreOrderedByTimestampDesc()
    {
        var account = await SeedAccountWithTransactions();

        var result = await _service.GenerateAnnualStatementAsync(account.Id);

        var txList = result!.Transactions.ToList();
        Assert.True(txList[0].Timestamp >= txList[1].Timestamp);
        Assert.True(txList[1].Timestamp >= txList[2].Timestamp);
    }

    [Fact]
    public async Task RunBatchReconciliationAsync_ReturnsCorrectTotals()
    {
        await SeedAccountWithTransactions();

        var result = await _service.RunBatchReconciliationAsync();

        Assert.Equal(1, result.TotalAccounts);
        Assert.Equal(3, result.TotalTransactions);
        Assert.Equal(3000m, result.TotalCredits);
        Assert.Equal(200m, result.TotalDebits);
        Assert.True(result.IsBalanced);
    }

    [Fact]
    public async Task RunBatchReconciliationAsync_EmptyDb_ReturnsZeros()
    {
        var result = await _service.RunBatchReconciliationAsync();

        Assert.Equal(0, result.TotalAccounts);
        Assert.Equal(0, result.TotalTransactions);
        Assert.Equal(0m, result.TotalCredits);
        Assert.Equal(0m, result.TotalDebits);
    }

    public void Dispose() => _db.Dispose();
}
