using ContosoBank.Data;
using ContosoBank.Metrics;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Services;

public class TransferService : ITransferService
{
    private readonly BankDbContext _db;
    private readonly ILogger<TransferService> _logger;
    private readonly BankMetrics _metrics;

    public TransferService(BankDbContext db, ILogger<TransferService> logger, BankMetrics metrics)
    {
        _db = db;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<IEnumerable<Transfer>> GetAllTransfersAsync()
    {
        _logger.LogInformation("Retrieving all transfers");
        return await _db.Transfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.RequestedAt)
            .ToListAsync();
    }

    public async Task<Transfer?> GetTransferByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving transfer {TransferId}", id);
        return await _db.Transfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TransferResult> CreateTransferAsync(TransferRequest request)
    {
        _logger.LogInformation("Processing transfer: {Amount} from account {FromAccountId} to account {ToAccountId}",
            request.Amount, request.FromAccountId, request.ToAccountId);

        if (request.FromAccountId == request.ToAccountId)
        {
            _logger.LogWarning("Transfer rejected: same source and destination account {AccountId}", request.FromAccountId);
            _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            return new TransferResult(false, null, "Source and destination accounts must be different.");
        }

        if (request.Amount <= 0)
        {
            _logger.LogWarning("Transfer rejected: invalid amount {Amount}", request.Amount);
            _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            return new TransferResult(false, null, "Transfer amount must be greater than zero.");
        }

        var fromAccount = await _db.Accounts.FindAsync(request.FromAccountId);
        if (fromAccount is null)
        {
            _logger.LogWarning("Transfer rejected: source account {AccountId} not found", request.FromAccountId);
            _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            return new TransferResult(false, null, "Source account not found.");
        }

        var toAccount = await _db.Accounts.FindAsync(request.ToAccountId);
        if (toAccount is null)
        {
            _logger.LogWarning("Transfer rejected: destination account {AccountId} not found", request.ToAccountId);
            _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            return new TransferResult(false, null, "Destination account not found.");
        }

        if (fromAccount.Balance < request.Amount)
        {
            _logger.LogWarning("Transfer rejected: insufficient funds in account {AccountId}. Balance: {Balance}, Requested: {Amount}",
                request.FromAccountId, fromAccount.Balance, request.Amount);
            _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "failed"));
            return new TransferResult(false, null, "Insufficient funds.");
        }

        var transfer = new Transfer
        {
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Amount = request.Amount,
            Status = TransferStatus.Processing,
            RequestedAt = DateTime.UtcNow
        };

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;
        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;

        _db.Transfers.Add(transfer);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Transfer {TransferId} completed: {Amount} from {FromAccount} to {ToAccount}",
            transfer.Id, transfer.Amount, fromAccount.AccountNumber, toAccount.AccountNumber);

        _metrics.TransfersTotal.Add(1, new KeyValuePair<string, object?>("status", "success"));

        return new TransferResult(true, transfer, null);
    }
}
