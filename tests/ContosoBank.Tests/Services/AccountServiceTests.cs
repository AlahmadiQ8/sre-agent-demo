using ContosoBank.Data;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Services;

public class AccountServiceTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BankDbContext(options);
        _db.Database.EnsureCreated();

        _service = new AccountService(_db, Mock.Of<ILogger<AccountService>>());
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsAllAccounts()
    {
        _db.Accounts.AddRange(
            new Account { AccountNumber = "CHK-001", AccountName = "Checking", Balance = 1000m },
            new Account { AccountNumber = "SAV-001", AccountName = "Savings", Balance = 5000m });
        await _db.SaveChangesAsync();

        var result = (await _service.GetAllAccountsAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsEmpty_WhenNoAccounts()
    {
        var result = (await _service.GetAllAccountsAsync()).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsSortedByName()
    {
        _db.Accounts.AddRange(
            new Account { AccountNumber = "CHK-002", AccountName = "Zeta Account", Balance = 100m },
            new Account { AccountNumber = "CHK-001", AccountName = "Alpha Account", Balance = 200m });
        await _db.SaveChangesAsync();

        var result = (await _service.GetAllAccountsAsync()).ToList();

        Assert.Equal("Alpha Account", result[0].AccountName);
        Assert.Equal("Zeta Account", result[1].AccountName);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsAccount_WhenExists()
    {
        var account = new Account { AccountNumber = "CHK-001", AccountName = "Test", Balance = 1000m };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var result = await _service.GetAccountByIdAsync(account.Id);

        Assert.NotNull(result);
        Assert.Equal("CHK-001", result.AccountNumber);
        Assert.Equal(1000m, result.Balance);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetAccountByIdAsync(999);

        Assert.Null(result);
    }

    public void Dispose() => _db.Dispose();
}
