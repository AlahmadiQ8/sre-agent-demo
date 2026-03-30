using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Models;

public enum TransactionType
{
    Credit,
    Debit
}

public enum TransactionStatus
{
    Completed,
    Pending,
    Failed
}

public class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public TransactionType Type { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    public Account Account { get; set; } = null!;
}
