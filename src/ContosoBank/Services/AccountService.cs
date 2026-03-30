using ContosoBank.Data;
using ContosoBank.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoBank.Services;

public class AccountService : IAccountService
{
    private readonly BankDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(BankDbContext db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Account>> GetAllAccountsAsync()
    {
        _logger.LogInformation("Retrieving all accounts");
        return await _db.Accounts.OrderBy(a => a.AccountName).ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving account {AccountId}", id);
        return await _db.Accounts.FindAsync(id);
    }
}
