using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IChaosService _chaosService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, IChaosService chaosService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _chaosService = chaosService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Chaos Scenario 4: Database Connection Failure — triggered by Refresh button
        // The ChaosDbInterceptor will throw if DbFailure is active, causing the DB query to fail
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            return Ok(accounts);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("simulated outage"))
        {
            _logger.LogError(ex, "Unable to load account information: database connection failed");
            return Problem(
                title: "Unable to load account information",
                detail: "A database connection error occurred. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("simulated outage"))
        {
            _logger.LogError(ex, "Unable to load account {AccountId}: database connection failed", id);
            return Problem(
                title: "Unable to load account information",
                detail: "A database connection error occurred. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        _logger.LogInformation("Account balance refresh requested");

        // Chaos Scenario 4: Database Connection Failure — activates the interceptor
        await _chaosService.TriggerDbConnectionFailure();

        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            return Ok(accounts);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("simulated outage"))
        {
            _logger.LogError(ex, "Unable to load account information: database connection failed");
            return Problem(
                title: "Unable to load account information",
                detail: "A database connection error occurred. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("fraud-detection")]
    public async Task<IActionResult> RunFraudDetection()
    {
        _logger.LogInformation("Fraud detection scan requested");

        // Chaos Scenario 2: CPU Spike — spawns CPU-intensive work on multiple threads
        await _chaosService.TriggerCpuSpike();

        return Ok(new
        {
            ScannedAt = DateTime.UtcNow,
            Status = "Scanning",
            Message = "Fraud detection scan initiated. Scanning transactions for suspicious activity..."
        });
    }
}
