using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IChaosService _chaosService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, IChaosService chaosService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _chaosService = chaosService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? accountId,
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (accountId.HasValue)
        {
            var accountTransactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId.Value);
            return Ok(accountTransactions);
        }

        if (category is not null || from.HasValue || to.HasValue)
        {
            var filtered = await _transactionService.SearchTransactionsAsync(category, from, to);
            return Ok(filtered);
        }

        var transactions = await _transactionService.GetAllTransactionsAsync();
        return Ok(transactions);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction is null)
        {
            return Problem(
                title: "Transaction not found",
                detail: $"No transaction exists with ID {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(transaction);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportFullHistory()
    {
        _logger.LogInformation("Full transaction history export requested");

        // Chaos Scenario 7: Log Flooding — writes thousands of verbose log entries per second
        await _chaosService.TriggerLogFlooding();

        var transactions = await _transactionService.GetAllTransactionsAsync();
        return Ok(new
        {
            ExportedAt = DateTime.UtcNow,
            TotalTransactions = transactions.Count(),
            Message = "Transaction history export completed."
        });
    }
}
