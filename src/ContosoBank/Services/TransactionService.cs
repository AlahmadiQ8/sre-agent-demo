using ContosoBank.Data;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Services;

public class TransactionService : ITransactionService
{
    private readonly BankDbContext _db;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(BankDbContext db, ILogger<TransactionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        _logger.LogInformation("Retrieving all transactions");
        return await _db.Transactions
            .Include(t => t.Account)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId)
    {
        _logger.LogInformation("Retrieving transactions for account {AccountId}", accountId);
        return await _db.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving transaction {TransactionId}", id);
        return await _db.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Transaction>> SearchTransactionsAsync(
        string? category = null, DateTime? from = null, DateTime? to = null)
    {
        _logger.LogInformation("Searching transactions: category={Category}, from={From}, to={To}",
            category, from, to);

        var query = _db.Transactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (from.HasValue)
            query = query.Where(t => t.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Timestamp <= to.Value);

        return await query
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }
}
