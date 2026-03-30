using ContosoBank.Models;

namespace ContosoBank.Services;

public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount);

public record TransferResult(bool Success, Transfer? Transfer, string? Error);

public interface ITransferService
{
    Task<IEnumerable<Transfer>> GetAllTransfersAsync();
    Task<Transfer?> GetTransferByIdAsync(int id);
    Task<TransferResult> CreateTransferAsync(TransferRequest request);
}
