using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpPost("annual-statement")]
    public async Task<IActionResult> AnnualStatement([FromQuery] int accountId)
    {
        _logger.LogInformation("Annual statement requested for account {AccountId}", accountId);

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

        var result = await _reportService.RunBatchReconciliationAsync();
        return Ok(result);
    }
}
