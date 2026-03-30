using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly IChaosService _chaosService;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(ITransferService transferService, IChaosService chaosService, ILogger<TransfersController> logger)
    {
        _transferService = transferService;
        _chaosService = chaosService;
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
        _logger.LogInformation("Processing wire transfer: {Amount} from {FromAccountId} to {ToAccountId}",
            request.Amount, request.FromAccountId, request.ToAccountId);

        // Chaos Scenario 3: HTTP 500 Errors — activates and immediately fails
        await _chaosService.TriggerHttpErrors();

        if (_chaosService.GetStatus().IsHttpErrorsActive)
        {
            _logger.LogError("Wire transfer failed: payment processor unavailable (simulated outage)");
            return Problem(
                title: "Wire transfer failed",
                detail: "The payment processor is currently unavailable. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

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
        _logger.LogInformation("Processing international transfer: {Amount} from {FromAccountId} to {ToAccountId}",
            request.Amount, request.FromAccountId, request.ToAccountId);

        // Chaos Scenario 5: Slow API / High Latency — activates then delays
        await _chaosService.TriggerSlowResponses();

        if (_chaosService.GetStatus().IsSlowResponsesActive)
        {
            _logger.LogWarning("International transfer experiencing high latency (simulated SWIFT delay)");
            await Task.Delay(30_000);
        }

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
