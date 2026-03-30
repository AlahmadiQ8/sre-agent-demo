using System.Diagnostics.Metrics;
using ContosoBank.Data;
using ContosoBank.Metrics;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Services;

public class TransferServiceTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly TransferService _service;

    public TransferServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BankDbContext(options);
        _db.Database.EnsureCreated();

        _service = new TransferService(_db, Mock.Of<ILogger<TransferService>>(),
            new BankMetrics(new TestMeterFactory()));
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }

    private async Task<(Account from, Account to)> SeedTwoAccounts(decimal fromBalance = 5000m, decimal toBalance = 1000m)
    {
        var from = new Account { AccountNumber = "CHK-001", AccountName = "From", Balance = fromBalance };
        var to = new Account { AccountNumber = "SAV-001", AccountName = "To", Balance = toBalance };
        _db.Accounts.AddRange(from, to);
        await _db.SaveChangesAsync();
        return (from, to);
    }

    [Fact]
    public async Task CreateTransferAsync_Success_UpdatesBalances()
    {
        var (from, to) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, to.Id, 1000m));

        Assert.True(result.Success);
        Assert.NotNull(result.Transfer);
        Assert.Null(result.Error);
        Assert.Equal(TransferStatus.Completed, result.Transfer.Status);
        Assert.Equal(4000m, from.Balance);
        Assert.Equal(2000m, to.Balance);
    }

    [Fact]
    public async Task CreateTransferAsync_Success_PersistsTransfer()
    {
        var (from, to) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, to.Id, 500m));

        var saved = await _db.Transfers.FindAsync(result.Transfer!.Id);
        Assert.NotNull(saved);
        Assert.Equal(500m, saved.Amount);
        Assert.NotNull(saved.CompletedAt);
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_InsufficientFunds()
    {
        var (from, to) = await SeedTwoAccounts(fromBalance: 100m);

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, to.Id, 500m));

        Assert.False(result.Success);
        Assert.Null(result.Transfer);
        Assert.Equal("Insufficient funds.", result.Error);
        Assert.Equal(100m, from.Balance); // balance unchanged
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_SameAccount()
    {
        var (from, _) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, from.Id, 100m));

        Assert.False(result.Success);
        Assert.Equal("Source and destination accounts must be different.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_InvalidAmount()
    {
        var (from, to) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, to.Id, 0m));

        Assert.False(result.Success);
        Assert.Equal("Transfer amount must be greater than zero.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_NegativeAmount()
    {
        var (from, to) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, to.Id, -50m));

        Assert.False(result.Success);
        Assert.Equal("Transfer amount must be greater than zero.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_SourceAccountNotFound()
    {
        var (_, to) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(999, to.Id, 100m));

        Assert.False(result.Success);
        Assert.Equal("Source account not found.", result.Error);
    }

    [Fact]
    public async Task CreateTransferAsync_Fails_DestinationAccountNotFound()
    {
        var (from, _) = await SeedTwoAccounts();

        var result = await _service.CreateTransferAsync(
            new TransferRequest(from.Id, 999, 100m));

        Assert.False(result.Success);
        Assert.Equal("Destination account not found.", result.Error);
    }

    [Fact]
    public async Task GetAllTransfersAsync_ReturnsAllTransfers()
    {
        var (from, to) = await SeedTwoAccounts();
        _db.Transfers.AddRange(
            new Transfer { FromAccountId = from.Id, ToAccountId = to.Id, Amount = 100m, Status = TransferStatus.Completed },
            new Transfer { FromAccountId = to.Id, ToAccountId = from.Id, Amount = 50m, Status = TransferStatus.Processing });
        await _db.SaveChangesAsync();

        var result = (await _service.GetAllTransfersAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTransferByIdAsync_ReturnsTransfer_WhenExists()
    {
        var (from, to) = await SeedTwoAccounts();
        var transfer = new Transfer { FromAccountId = from.Id, ToAccountId = to.Id, Amount = 200m, Status = TransferStatus.Completed };
        _db.Transfers.Add(transfer);
        await _db.SaveChangesAsync();

        var result = await _service.GetTransferByIdAsync(transfer.Id);

        Assert.NotNull(result);
        Assert.Equal(200m, result.Amount);
    }

    [Fact]
    public async Task GetTransferByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetTransferByIdAsync(999);

        Assert.Null(result);
    }

    public void Dispose() => _db.Dispose();
}
