using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Data;

public static class SeedData
{
    public static void Initialize(BankDbContext context)
    {
        if (context.Accounts.Any())
            return;

        var now = DateTime.UtcNow;

        // 5 accounts: 2 checking, 1 savings, 1 credit, 1 business (checking)
        var accounts = new List<Account>
        {
            new()
            {
                AccountNumber = "CHK-100001",
                AccountName = "Primary Checking",
                AccountType = AccountType.Checking,
                Balance = 12_450.75m,
                Currency = "USD",
                CreatedAt = now.AddMonths(-6)
            },
            new()
            {
                AccountNumber = "CHK-100002",
                AccountName = "Joint Checking",
                AccountType = AccountType.Checking,
                Balance = 8_320.50m,
                Currency = "USD",
                CreatedAt = now.AddMonths(-4)
            },
            new()
            {
                AccountNumber = "SAV-200001",
                AccountName = "High-Yield Savings",
                AccountType = AccountType.Savings,
                Balance = 45_000.00m,
                Currency = "USD",
                CreatedAt = now.AddMonths(-12)
            },
            new()
            {
                AccountNumber = "CRD-300001",
                AccountName = "Platinum Credit Card",
                AccountType = AccountType.Credit,
                Balance = 2_150.30m,
                Currency = "USD",
                CreatedAt = now.AddMonths(-8)
            },
            new()
            {
                AccountNumber = "BIZ-400001",
                AccountName = "Business Account",
                AccountType = AccountType.Checking,
                Balance = 67_890.25m,
                Currency = "USD",
                CreatedAt = now.AddMonths(-3)
            }
        };

        context.Accounts.AddRange(accounts);
        context.SaveChanges();

        // Seed transactions across the past 30 days
        var random = new Random(42); // Fixed seed for reproducible data
        var categories = new[] { "Groceries", "Utilities", "Payroll", "Rent", "Entertainment", "Dining", "Transport", "Healthcare", "Insurance", "Subscription" };
        var descriptions = new Dictionary<string, string[]>
        {
            ["Groceries"] = ["Whole Foods Market", "Trader Joe's", "Costco Wholesale", "Safeway"],
            ["Utilities"] = ["Electric Bill - ConEd", "Water & Sewer", "Internet - Comcast", "Gas Bill - National Grid"],
            ["Payroll"] = ["Salary Deposit - Contoso Ltd", "Bonus Payment", "Freelance Payment"],
            ["Rent"] = ["Monthly Rent Payment", "Parking Space Rental"],
            ["Entertainment"] = ["Netflix Subscription", "Spotify Premium", "Movie Tickets - AMC", "Concert Tickets"],
            ["Dining"] = ["Starbucks Coffee", "Chipotle Mexican Grill", "The Capital Grille", "DoorDash Delivery"],
            ["Transport"] = ["Uber Ride", "Metro Card Refill", "Gas Station - Shell", "Parking Fee"],
            ["Healthcare"] = ["Pharmacy - CVS", "Doctor Visit Copay", "Dental Cleaning"],
            ["Insurance"] = ["Auto Insurance Premium", "Health Insurance Premium"],
            ["Subscription"] = ["Adobe Creative Cloud", "Microsoft 365", "AWS Monthly Bill"]
        };

        var transactions = new List<Transaction>();
        foreach (var account in accounts)
        {
            var txCount = random.Next(20, 30);
            for (var i = 0; i < txCount; i++)
            {
                var category = categories[random.Next(categories.Length)];
                var descs = descriptions[category];
                var desc = descs[random.Next(descs.Length)];
                var isCredit = category == "Payroll" || random.NextDouble() < 0.25;
                var amount = category switch
                {
                    "Payroll" => Math.Round((decimal)(random.NextDouble() * 4000 + 2000), 2),
                    "Rent" => Math.Round((decimal)(random.NextDouble() * 500 + 1500), 2),
                    "Insurance" => Math.Round((decimal)(random.NextDouble() * 200 + 100), 2),
                    _ => Math.Round((decimal)(random.NextDouble() * 150 + 5), 2)
                };

                transactions.Add(new Transaction
                {
                    AccountId = account.Id,
                    Type = isCredit ? TransactionType.Credit : TransactionType.Debit,
                    Amount = amount,
                    Description = desc,
                    Category = category,
                    Timestamp = now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24)).AddMinutes(-random.Next(0, 60)),
                    Status = random.NextDouble() < 0.95 ? TransactionStatus.Completed : TransactionStatus.Pending
                });
            }
        }

        context.Transactions.AddRange(transactions);
        context.SaveChanges();

        // Seed recent transfers
        var transfers = new List<Transfer>
        {
            new()
            {
                FromAccountId = accounts[0].Id,
                ToAccountId = accounts[2].Id,
                Amount = 500.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-2),
                CompletedAt = now.AddDays(-2).AddMinutes(1)
            },
            new()
            {
                FromAccountId = accounts[4].Id,
                ToAccountId = accounts[0].Id,
                Amount = 3_000.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-5),
                CompletedAt = now.AddDays(-5).AddMinutes(2)
            },
            new()
            {
                FromAccountId = accounts[1].Id,
                ToAccountId = accounts[2].Id,
                Amount = 1_000.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-7),
                CompletedAt = now.AddDays(-7).AddMinutes(1)
            },
            new()
            {
                FromAccountId = accounts[0].Id,
                ToAccountId = accounts[1].Id,
                Amount = 250.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-10),
                CompletedAt = now.AddDays(-10).AddMinutes(1)
            },
            new()
            {
                FromAccountId = accounts[2].Id,
                ToAccountId = accounts[4].Id,
                Amount = 5_000.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-12),
                CompletedAt = now.AddDays(-12).AddMinutes(3)
            },
            new()
            {
                FromAccountId = accounts[0].Id,
                ToAccountId = accounts[3].Id,
                Amount = 750.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-15),
                CompletedAt = now.AddDays(-15).AddMinutes(1)
            },
            new()
            {
                FromAccountId = accounts[4].Id,
                ToAccountId = accounts[1].Id,
                Amount = 2_500.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-18),
                CompletedAt = now.AddDays(-18).AddMinutes(2)
            },
            new()
            {
                FromAccountId = accounts[1].Id,
                ToAccountId = accounts[0].Id,
                Amount = 100.00m,
                Status = TransferStatus.Processing,
                RequestedAt = now.AddMinutes(-30)
            },
            new()
            {
                FromAccountId = accounts[0].Id,
                ToAccountId = accounts[4].Id,
                Amount = 1_500.00m,
                Status = TransferStatus.Failed,
                RequestedAt = now.AddDays(-1),
                CompletedAt = now.AddDays(-1).AddMinutes(5)
            },
            new()
            {
                FromAccountId = accounts[3].Id,
                ToAccountId = accounts[2].Id,
                Amount = 300.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-20),
                CompletedAt = now.AddDays(-20).AddMinutes(1)
            },
            new()
            {
                FromAccountId = accounts[4].Id,
                ToAccountId = accounts[2].Id,
                Amount = 10_000.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-25),
                CompletedAt = now.AddDays(-25).AddMinutes(4)
            },
            new()
            {
                FromAccountId = accounts[0].Id,
                ToAccountId = accounts[2].Id,
                Amount = 200.00m,
                Status = TransferStatus.Completed,
                RequestedAt = now.AddDays(-3),
                CompletedAt = now.AddDays(-3).AddMinutes(1)
            }
        };

        context.Transfers.AddRange(transfers);
        context.SaveChanges();
    }
}
