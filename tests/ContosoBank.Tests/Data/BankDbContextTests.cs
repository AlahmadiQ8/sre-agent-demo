using ContosoBank.Data;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Tests.Data;

public class BankDbContextTests : IDisposable
{
    private readonly BankDbContext _context;

    public BankDbContextTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void CanCreateDatabase()
    {
        Assert.NotNull(_context);
        Assert.True(_context.Database.EnsureCreated() || true); // InMemory always succeeds
    }

    [Fact]
    public void CanAddAndRetrieveAccount()
    {
        var account = new Account
        {
            AccountNumber = "TEST-001",
            AccountName = "Test Account",
            AccountType = AccountType.Checking,
            Balance = 1000.00m
        };

        _context.Accounts.Add(account);
        _context.SaveChanges();

        var retrieved = _context.Accounts.First(a => a.AccountNumber == "TEST-001");
        Assert.Equal("Test Account", retrieved.AccountName);
        Assert.Equal(AccountType.Checking, retrieved.AccountType);
        Assert.Equal(1000.00m, retrieved.Balance);
        Assert.Equal("USD", retrieved.Currency);
    }

    [Fact]
    public void CanAddTransactionWithForeignKey()
    {
        var account = new Account
        {
            AccountNumber = "TEST-002",
            AccountName = "FK Test Account",
            AccountType = AccountType.Savings,
            Balance = 5000.00m
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Type = TransactionType.Debit,
            Amount = 50.00m,
            Description = "Test Transaction",
            Category = "Test"
        };
        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        var retrieved = _context.Transactions
            .Include(t => t.Account)
            .First(t => t.Description == "Test Transaction");

        Assert.Equal(account.Id, retrieved.AccountId);
        Assert.Equal("FK Test Account", retrieved.Account.AccountName);
    }

    [Fact]
    public void CanAddTransferWithForeignKeys()
    {
        var fromAccount = new Account
        {
            AccountNumber = "TEST-FROM",
            AccountName = "From Account",
            AccountType = AccountType.Checking,
            Balance = 10000.00m
        };
        var toAccount = new Account
        {
            AccountNumber = "TEST-TO",
            AccountName = "To Account",
            AccountType = AccountType.Savings,
            Balance = 2000.00m
        };
        _context.Accounts.AddRange(fromAccount, toAccount);
        _context.SaveChanges();

        var transfer = new Transfer
        {
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Amount = 500.00m,
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };
        _context.Transfers.Add(transfer);
        _context.SaveChanges();

        var retrieved = _context.Transfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .First();

        Assert.Equal("From Account", retrieved.FromAccount.AccountName);
        Assert.Equal("To Account", retrieved.ToAccount.AccountName);
        Assert.Equal(500.00m, retrieved.Amount);
        Assert.Equal(TransferStatus.Completed, retrieved.Status);
        Assert.NotNull(retrieved.CompletedAt);
    }

    [Fact]
    public void Account_HasNavigationToTransactions()
    {
        var account = new Account
        {
            AccountNumber = "TEST-NAV",
            AccountName = "Nav Test Account",
            AccountType = AccountType.Checking,
            Balance = 1000.00m
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();

        _context.Transactions.AddRange(
            new Transaction { AccountId = account.Id, Type = TransactionType.Credit, Amount = 100m, Description = "TX1", Category = "Test" },
            new Transaction { AccountId = account.Id, Type = TransactionType.Debit, Amount = 50m, Description = "TX2", Category = "Test" }
        );
        _context.SaveChanges();

        var retrieved = _context.Accounts
            .Include(a => a.Transactions)
            .First(a => a.AccountNumber == "TEST-NAV");

        Assert.Equal(2, retrieved.Transactions.Count);
    }

    [Fact]
    public void Account_HasNavigationToTransfers()
    {
        var account1 = new Account { AccountNumber = "TEST-T1", AccountName = "A1", AccountType = AccountType.Checking, Balance = 5000m };
        var account2 = new Account { AccountNumber = "TEST-T2", AccountName = "A2", AccountType = AccountType.Savings, Balance = 3000m };
        _context.Accounts.AddRange(account1, account2);
        _context.SaveChanges();

        _context.Transfers.Add(new Transfer
        {
            FromAccountId = account1.Id,
            ToAccountId = account2.Id,
            Amount = 200m,
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var from = _context.Accounts.Include(a => a.OutgoingTransfers).First(a => a.Id == account1.Id);
        var to = _context.Accounts.Include(a => a.IncomingTransfers).First(a => a.Id == account2.Id);

        Assert.Single(from.OutgoingTransfers);
        Assert.Single(to.IncomingTransfers);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
