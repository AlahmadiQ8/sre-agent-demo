namespace ContosoBank.Services;

public record AccountStatement(
    int AccountId,
    string AccountNumber,
    string AccountName,
    decimal Balance,
    DateTime GeneratedAt,
    IEnumerable<TransactionSummary> Transactions);

public record TransactionSummary(
    int Id,
    string Type,
    decimal Amount,
    string Description,
    DateTime Timestamp);

public record ReconciliationResult(
    DateTime GeneratedAt,
    int TotalAccounts,
    int TotalTransactions,
    decimal TotalCredits,
    decimal TotalDebits,
    bool IsBalanced);

public interface IReportService
{
    Task<AccountStatement?> GenerateAnnualStatementAsync(int accountId);
    Task<ReconciliationResult> RunBatchReconciliationAsync();
}
