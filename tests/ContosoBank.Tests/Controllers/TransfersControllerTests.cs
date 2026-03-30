using ContosoBank.Controllers;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class TransfersControllerTests
{
    private readonly Mock<ITransferService> _mockService;
    private readonly Mock<IChaosService> _mockChaos;
    private readonly TransfersController _controller;

    public TransfersControllerTests()
    {
        _mockService = new Mock<ITransferService>();
        _mockChaos = new Mock<IChaosService>();
        _mockChaos.Setup(c => c.GetStatus()).Returns(new ChaosStatus());
        _controller = new TransfersController(_mockService.Object, _mockChaos.Object, Mock.Of<ILogger<TransfersController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTransfers()
    {
        var transfers = new List<Transfer>
        {
            new() { Id = 1, Amount = 100m, Status = TransferStatus.Completed }
        };
        _mockService.Setup(s => s.GetAllTransfersAsync()).ReturnsAsync(transfers);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetTransferByIdAsync(999)).ReturnsAsync((Transfer?)null);

        var result = await _controller.GetById(999);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 500m);
        var transfer = new Transfer { Id = 1, FromAccountId = 1, ToAccountId = 2, Amount = 500m, Status = TransferStatus.Completed };
        _mockService.Setup(s => s.CreateTransferAsync(request))
            .ReturnsAsync(new TransferResult(true, transfer, null));

        var result = await _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400ProblemDetails_OnFailure()
    {
        var request = new TransferRequest(1, 1, 500m);
        _mockService.Setup(s => s.CreateTransferAsync(request))
            .ReturnsAsync(new TransferResult(false, null, "Source and destination accounts must be different."));

        var result = await _controller.Create(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task WireTransfer_ReturnsCreated_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 1000m);
        var transfer = new Transfer { Id = 2, FromAccountId = 1, ToAccountId = 2, Amount = 1000m, Status = TransferStatus.Completed };
        _mockService.Setup(s => s.CreateTransferAsync(request))
            .ReturnsAsync(new TransferResult(true, transfer, null));

        var result = await _controller.WireTransfer(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task WireTransfer_Returns400_OnFailure()
    {
        var request = new TransferRequest(1, 2, 0m);
        _mockService.Setup(s => s.CreateTransferAsync(request))
            .ReturnsAsync(new TransferResult(false, null, "Transfer amount must be greater than zero."));

        var result = await _controller.WireTransfer(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task InternationalTransfer_ReturnsCreated_OnSuccess()
    {
        var request = new TransferRequest(1, 2, 2000m);
        var transfer = new Transfer { Id = 3, FromAccountId = 1, ToAccountId = 2, Amount = 2000m, Status = TransferStatus.Completed };
        _mockService.Setup(s => s.CreateTransferAsync(request))
            .ReturnsAsync(new TransferResult(true, transfer, null));

        var result = await _controller.InternationalTransfer(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }
}
