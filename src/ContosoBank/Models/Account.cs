using System.ComponentModel.DataAnnotations;

namespace ContosoBank.Models;

public enum AccountType
{
    Checking,
    Savings,
    Credit
}

public class Account
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AccountName { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Transfer> OutgoingTransfers { get; set; } = new List<Transfer>();
    public ICollection<Transfer> IncomingTransfers { get; set; } = new List<Transfer>();
}
