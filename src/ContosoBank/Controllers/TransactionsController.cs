using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
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
}
