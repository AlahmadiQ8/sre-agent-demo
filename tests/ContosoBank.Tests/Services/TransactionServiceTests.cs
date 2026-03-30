using ContosoBank.Data;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BankDbContext(options);
        _db.Database.EnsureCreated();

        _service = new TransactionService(_db, Mock.Of<ILogger<TransactionService>>());
    }

    private async Task<Account> SeedAccountWithTransactions()
    {
        var account = new Account { AccountNumber = "CHK-001", AccountName = "Test", Balance = 5000m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _db.Transactions.AddRange(
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Debit, Amount = 50m,
                Description = "Coffee", Category = "Dining",
                Timestamp = DateTime.UtcNow.AddDays(-1)
            },
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Credit, Amount = 3000m,
                Description = "Salary", Category = "Payroll",
                Timestamp = DateTime.UtcNow.AddDays(-5)
            },
            new Transaction
            {
                AccountId = account.Id, Type = TransactionType.Debit, Amount = 120m,
                Description = "Electric Bill", Category = "Utilities",
                Timestamp = DateTime.UtcNow.AddDays(-10)
            });
        await _db.SaveChangesAsync();

        return account;
    }

    [Fact]
    public async Task GetAllTransactionsAsync_ReturnsAll()
    {
        await SeedAccountWithTransactions();

        var result = (await _service.GetAllTransactionsAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_ReturnsSortedByTimestampDesc()
    {
        await SeedAccountWithTransactions();

        var result = (await _service.GetAllTransactionsAsync()).ToList();

        Assert.True(result[0].Timestamp >= result[1].Timestamp);
        Assert.True(result[1].Timestamp >= result[2].Timestamp);
    }

    [Fact]
    public async Task GetTransactionsByAccountIdAsync_ReturnsOnlyForAccount()
    {
        var account = await SeedAccountWithTransactions();
        var other = new Account { AccountNumber = "SAV-001", AccountName = "Other", Balance = 1000m };
        _db.Accounts.Add(other);
        await _db.SaveChangesAsync();
        _db.Transactions.Add(new Transaction
        {
            AccountId = other.Id, Type = TransactionType.Debit, Amount = 25m,
            Description = "Lunch", Category = "Dining", Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = (await _service.GetTransactionsByAccountIdAsync(account.Id)).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.Equal(account.Id, t.AccountId));
    }

    [Fact]
    public async Task GetTransactionByIdAsync_ReturnsTransaction_WhenExists()
    {
        var account = await SeedAccountWithTransactions();
        var tx = await _db.Transactions.FirstAsync();

        var result = await _service.GetTransactionByIdAsync(tx.Id);

        Assert.NotNull(result);
        Assert.Equal(tx.Description, result.Description);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetTransactionByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchTransactionsAsync_FiltersByCategory()
    {
        await SeedAccountWithTransactions();

        var result = (await _service.SearchTransactionsAsync(category: "Dining")).ToList();

        Assert.Single(result);
        Assert.Equal("Dining", result[0].Category);
    }

    [Fact]
    public async Task SearchTransactionsAsync_FiltersByDateRange()
    {
        await SeedAccountWithTransactions();

        var from = DateTime.UtcNow.AddDays(-6);
        var to = DateTime.UtcNow.AddDays(-4);
        var result = (await _service.SearchTransactionsAsync(from: from, to: to)).ToList();

        Assert.Single(result);
        Assert.Equal("Salary", result[0].Description);
    }

    [Fact]
    public async Task SearchTransactionsAsync_NoFilters_ReturnsAll()
    {
        await SeedAccountWithTransactions();

        var result = (await _service.SearchTransactionsAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    public void Dispose() => _db.Dispose();
}
