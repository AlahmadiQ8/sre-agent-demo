using ContosoBank.Models;

namespace ContosoBank.Services;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetAllTransactionsAsync();
    Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId);
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<IEnumerable<Transaction>> SearchTransactionsAsync(string? category = null, DateTime? from = null, DateTime? to = null);
}
