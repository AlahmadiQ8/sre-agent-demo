using ContosoBank.Controllers;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _mockService;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _mockService = new Mock<IReportService>();
        _controller = new ReportsController(_mockService.Object, Mock.Of<ILogger<ReportsController>>());
    }

    [Fact]
    public async Task AnnualStatement_ReturnsOk_WhenFound()
    {
        var statement = new AccountStatement(1, "CHK-001", "Test", 5000m, DateTime.UtcNow, []);
        _mockService.Setup(s => s.GenerateAnnualStatementAsync(1)).ReturnsAsync(statement);

        var result = await _controller.AnnualStatement(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<AccountStatement>(ok.Value);
        Assert.Equal("CHK-001", returned.AccountNumber);
    }

    [Fact]
    public async Task AnnualStatement_Returns404_WhenAccountNotFound()
    {
        _mockService.Setup(s => s.GenerateAnnualStatementAsync(999)).ReturnsAsync((AccountStatement?)null);

        var result = await _controller.AnnualStatement(999);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task Reconciliation_ReturnsOk()
    {
        var recon = new ReconciliationResult(DateTime.UtcNow, 5, 100, 50000m, 45000m, true);
        _mockService.Setup(s => s.RunBatchReconciliationAsync()).ReturnsAsync(recon);

        var result = await _controller.Reconciliation();

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<ReconciliationResult>(ok.Value);
        Assert.Equal(5, returned.TotalAccounts);
    }
}
