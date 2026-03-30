using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Models;

public enum TransferStatus
{
    Processing,
    Completed,
    Failed
}

public class Transfer
{
    public int Id { get; set; }

    public int FromAccountId { get; set; }

    public int ToAccountId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Processing;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public Account FromAccount { get; set; } = null!;
    public Account ToAccount { get; set; } = null!;
}
