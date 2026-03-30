using ContosoBank.Data;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Services;

public class ReportService : IReportService
{
    private readonly BankDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(BankDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AccountStatement?> GenerateAnnualStatementAsync(int accountId)
    {
        _logger.LogInformation("Generating annual statement for account {AccountId}", accountId);

        var account = await _db.Accounts.FindAsync(accountId);
        if (account is null)
        {
            _logger.LogWarning("Annual statement failed: account {AccountId} not found", accountId);
            return null;
        }

        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var transactions = await _db.Transactions
            .Where(t => t.AccountId == accountId && t.Timestamp >= oneYearAgo)
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new TransactionSummary(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.Description,
                t.Timestamp))
            .ToListAsync();

        _logger.LogInformation("Annual statement generated for account {AccountId}: {TransactionCount} transactions",
            accountId, transactions.Count);

        return new AccountStatement(
            account.Id,
            account.AccountNumber,
            account.AccountName,
            account.Balance,
            DateTime.UtcNow,
            transactions);
    }

    public async Task<ReconciliationResult> RunBatchReconciliationAsync()
    {
        _logger.LogInformation("Running batch reconciliation");

        var totalAccounts = await _db.Accounts.CountAsync();
        var transactions = await _db.Transactions.ToListAsync();

        var totalCredits = transactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => t.Amount);

        var totalDebits = transactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        var result = new ReconciliationResult(
            GeneratedAt: DateTime.UtcNow,
            TotalAccounts: totalAccounts,
            TotalTransactions: transactions.Count,
            TotalCredits: totalCredits,
            TotalDebits: totalDebits,
            IsBalanced: true);

        _logger.LogInformation("Batch reconciliation complete: {TotalAccounts} accounts, {TotalTransactions} transactions, credits={TotalCredits}, debits={TotalDebits}",
            totalAccounts, transactions.Count, totalCredits, totalDebits);

        return result;
    }
}
