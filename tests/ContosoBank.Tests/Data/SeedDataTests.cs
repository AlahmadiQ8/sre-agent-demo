using ContosoBank.Data;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Tests.Data;

public class SeedDataTests : IDisposable
{
    private readonly BankDbContext _context;

    public SeedDataTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Initialize_Seeds5Accounts()
    {
        SeedData.Initialize(_context);

        Assert.Equal(5, _context.Accounts.Count());
    }

    [Fact]
    public void Initialize_SeedsCorrectAccountTypes()
    {
        SeedData.Initialize(_context);

        var accounts = _context.Accounts.ToList();

        // 2 checking (Primary + Business), 1 savings, 1 credit, + joint checking = 3 checking total
        Assert.Equal(3, accounts.Count(a => a.AccountType == AccountType.Checking));
        Assert.Single(accounts.Where(a => a.AccountType == AccountType.Savings));
        Assert.Single(accounts.Where(a => a.AccountType == AccountType.Credit));
    }

    [Fact]
    public void Initialize_SeedsAccountsWithUniqueNumbers()
    {
        SeedData.Initialize(_context);

        var accountNumbers = _context.Accounts.Select(a => a.AccountNumber).ToList();
        Assert.Equal(accountNumbers.Count, accountNumbers.Distinct().Count());
    }

    [Fact]
    public void Initialize_SeedsAccountsWithPositiveBalances()
    {
        SeedData.Initialize(_context);

        Assert.All(_context.Accounts.ToList(), a => Assert.True(a.Balance > 0));
    }

    [Fact]
    public void Initialize_SeedsTransactions()
    {
        SeedData.Initialize(_context);

        // Each account gets 20-30 transactions, 5 accounts = at least 100
        Assert.True(_context.Transactions.Count() >= 100);
    }

    [Fact]
    public void Initialize_SeedsTransactionsWithValidForeignKeys()
    {
        SeedData.Initialize(_context);

        var accountIds = _context.Accounts.Select(a => a.Id).ToHashSet();
        var transactions = _context.Transactions.ToList();

        Assert.All(transactions, t => Assert.Contains(t.AccountId, accountIds));
    }

    [Fact]
    public void Initialize_SeedsTransfers()
    {
        SeedData.Initialize(_context);

        Assert.True(_context.Transfers.Count() >= 10);
    }

    [Fact]
    public void Initialize_SeedsTransfersWithValidAccounts()
    {
        SeedData.Initialize(_context);

        var accountIds = _context.Accounts.Select(a => a.Id).ToHashSet();
        var transfers = _context.Transfers.ToList();

        Assert.All(transfers, t =>
        {
            Assert.Contains(t.FromAccountId, accountIds);
            Assert.Contains(t.ToAccountId, accountIds);
            Assert.NotEqual(t.FromAccountId, t.ToAccountId);
        });
    }

    [Fact]
    public void Initialize_SeedsTransfersWithMixedStatuses()
    {
        SeedData.Initialize(_context);

        var statuses = _context.Transfers.Select(t => t.Status).Distinct().ToList();
        Assert.Contains(TransferStatus.Completed, statuses);
        Assert.Contains(TransferStatus.Processing, statuses);
        Assert.Contains(TransferStatus.Failed, statuses);
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        SeedData.Initialize(_context);
        var count1 = _context.Accounts.Count();

        SeedData.Initialize(_context);
        var count2 = _context.Accounts.Count();

        Assert.Equal(count1, count2);
    }

    [Fact]
    public void Initialize_AllAccountsHaveUsdCurrency()
    {
        SeedData.Initialize(_context);

        Assert.All(_context.Accounts.ToList(), a => Assert.Equal("USD", a.Currency));
    }

    [Fact]
    public void Initialize_TransactionsHaveDescriptionsAndCategories()
    {
        SeedData.Initialize(_context);

        var transactions = _context.Transactions.ToList();
        Assert.All(transactions, t =>
        {
            Assert.NotEmpty(t.Description);
            Assert.NotEmpty(t.Category);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
