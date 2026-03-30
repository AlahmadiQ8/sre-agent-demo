using ContosoBank.Models;

namespace ContosoBank.Tests.Models;

public class AccountTests
{
    [Fact]
    public void Account_DefaultValues_AreCorrect()
    {
        var account = new Account();

        Assert.Equal(string.Empty, account.AccountNumber);
        Assert.Equal(string.Empty, account.AccountName);
        Assert.Equal(AccountType.Checking, account.AccountType);
        Assert.Equal(0m, account.Balance);
        Assert.Equal("USD", account.Currency);
        Assert.Empty(account.Transactions);
        Assert.NotNull(account.OutgoingTransfers);
        Assert.NotNull(account.IncomingTransfers);
    }

    [Theory]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Credit)]
    public void AccountType_AllValues_AreDefined(AccountType type)
    {
        var account = new Account { AccountType = type };
        Assert.Equal(type, account.AccountType);
    }
}

public class TransactionTests
{
    [Fact]
    public void Transaction_DefaultValues_AreCorrect()
    {
        var transaction = new Transaction();

        Assert.Equal(string.Empty, transaction.Description);
        Assert.Equal(string.Empty, transaction.Category);
        Assert.Equal(TransactionStatus.Completed, transaction.Status);
        Assert.Equal(TransactionType.Credit, transaction.Type);
    }

    [Theory]
    [InlineData(TransactionType.Credit)]
    [InlineData(TransactionType.Debit)]
    public void TransactionType_AllValues_AreDefined(TransactionType type)
    {
        var transaction = new Transaction { Type = type };
        Assert.Equal(type, transaction.Type);
    }

    [Theory]
    [InlineData(TransactionStatus.Completed)]
    [InlineData(TransactionStatus.Pending)]
    [InlineData(TransactionStatus.Failed)]
    public void TransactionStatus_AllValues_AreDefined(TransactionStatus status)
    {
        var transaction = new Transaction { Status = status };
        Assert.Equal(status, transaction.Status);
    }
}

public class TransferTests
{
    [Fact]
    public void Transfer_DefaultValues_AreCorrect()
    {
        var transfer = new Transfer();

        Assert.Equal(TransferStatus.Processing, transfer.Status);
        Assert.Null(transfer.CompletedAt);
        Assert.Equal(0m, transfer.Amount);
    }

    [Theory]
    [InlineData(TransferStatus.Processing)]
    [InlineData(TransferStatus.Completed)]
    [InlineData(TransferStatus.Failed)]
    public void TransferStatus_AllValues_AreDefined(TransferStatus status)
    {
        var transfer = new Transfer { Status = status };
        Assert.Equal(status, transfer.Status);
    }
}
