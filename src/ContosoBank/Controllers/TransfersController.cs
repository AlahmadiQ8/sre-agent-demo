using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(ITransferService transferService, ILogger<TransfersController> logger)
    {
        _transferService = transferService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transfers = await _transferService.GetAllTransfersAsync();
        return Ok(transfers);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transfer = await _transferService.GetTransferByIdAsync(id);
        if (transfer is null)
        {
            return Problem(
                title: "Transfer not found",
                detail: $"No transfer exists with ID {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(transfer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferRequest request)
    {
        var result = await _transferService.CreateTransferAsync(request);
        if (!result.Success)
        {
            return Problem(
                title: "Transfer failed",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Transfer!.Id }, result.Transfer);
    }

    [HttpPost("wire")]
    public async Task<IActionResult> WireTransfer([FromBody] TransferRequest request)
    {
        // Wire transfers use the same internal transfer model — this endpoint
        // exists as a chaos trigger (Scenario 3: HTTP 500 Errors)
        _logger.LogInformation("Processing wire transfer: {Amount} from {FromAccountId} to {ToAccountId}",
            request.Amount, request.FromAccountId, request.ToAccountId);

        var result = await _transferService.CreateTransferAsync(request);
        if (!result.Success)
        {
            return Problem(
                title: "Wire transfer failed",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Transfer!.Id }, result.Transfer);
    }

    [HttpPost("international")]
    public async Task<IActionResult> InternationalTransfer([FromBody] TransferRequest request)
    {
        // International transfers use the same internal transfer model — this endpoint
        // exists as a chaos trigger (Scenario 5: Slow API / High Latency)
        _logger.LogInformation("Processing international transfer: {Amount} from {FromAccountId} to {ToAccountId}",
            request.Amount, request.FromAccountId, request.ToAccountId);

        var result = await _transferService.CreateTransferAsync(request);
        if (!result.Success)
        {
            return Problem(
                title: "International transfer failed",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Transfer!.Id }, result.Transfer);
    }
}
