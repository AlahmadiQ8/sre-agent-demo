using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.Balance).HasPrecision(18, 2);
            entity.Property(a => a.AccountType).HasConversion<string>();
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(t => t.AccountId);
            entity.HasIndex(t => t.Timestamp);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Type).HasConversion<string>();
            entity.Property(t => t.Status).HasConversion<string>();

            entity.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transfer configuration
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasIndex(t => t.FromAccountId);
            entity.HasIndex(t => t.ToAccountId);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Status).HasConversion<string>();

            entity.HasOne(t => t.FromAccount)
                .WithMany(a => a.OutgoingTransfers)
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToAccount)
                .WithMany(a => a.IncomingTransfers)
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
