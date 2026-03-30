using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await _accountService.GetAllAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account is null)
        {
            _logger.LogWarning("Account {AccountId} not found", id);
            return Problem(
                title: "Account not found",
                detail: $"No account exists with ID {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(account);
    }
}
