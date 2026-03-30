using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IChaosService _chaosService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, IChaosService chaosService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _chaosService = chaosService;
        _logger = logger;
    }

    [HttpPost("annual-statement")]
    public async Task<IActionResult> AnnualStatement([FromQuery] int accountId)
    {
        _logger.LogInformation("Annual statement requested for account {AccountId}", accountId);

        // Chaos Scenario 1: Memory Leak — allocates large byte arrays in a loop
        await _chaosService.TriggerMemoryLeak();

        var statement = await _reportService.GenerateAnnualStatementAsync(accountId);
        if (statement is null)
        {
            return Problem(
                title: "Statement generation failed",
                detail: $"No account exists with ID {accountId}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(statement);
    }

    [HttpPost("reconciliation")]
    public async Task<IActionResult> Reconciliation()
    {
        _logger.LogInformation("Batch reconciliation requested");

        // Chaos Scenario 8: Exception Storm — spawns parallel tasks that throw random exceptions
        await _chaosService.TriggerExceptionStorm();

        if (_chaosService.GetStatus().IsExceptionStormActive)
        {
            _logger.LogError("Batch reconciliation failed due to exception storm");
            return Problem(
                title: "Reconciliation failed",
                detail: "An error occurred during batch reconciliation processing.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var result = await _reportService.RunBatchReconciliationAsync();
        return Ok(result);
    }
}
