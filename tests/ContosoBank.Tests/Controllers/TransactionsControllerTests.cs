using ContosoBank.Controllers;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _mockService;
    private readonly Mock<IChaosService> _mockChaos;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _mockService = new Mock<ITransactionService>();
        _mockChaos = new Mock<IChaosService>();
        _mockChaos.Setup(c => c.GetStatus()).Returns(new ChaosStatus());
        _controller = new TransactionsController(_mockService.Object, _mockChaos.Object, Mock.Of<ILogger<TransactionsController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAllTransactions()
    {
        var transactions = new List<Transaction>
        {
            new() { Id = 1, Description = "Test", Amount = 50m }
        };
        _mockService.Setup(s => s.GetAllTransactionsAsync()).ReturnsAsync(transactions);

        var result = await _controller.GetAll(null, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetAll_WithAccountId_FiltersbyAccount()
    {
        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = 5, Description = "Test", Amount = 50m }
        };
        _mockService.Setup(s => s.GetTransactionsByAccountIdAsync(5)).ReturnsAsync(transactions);

        var result = await _controller.GetAll(5, null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        _mockService.Verify(s => s.GetTransactionsByAccountIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithCategory_SearchesTransactions()
    {
        var transactions = new List<Transaction>
        {
            new() { Id = 1, Category = "Dining", Description = "Coffee", Amount = 5m }
        };
        _mockService.Setup(s => s.SearchTransactionsAsync("Dining", null, null)).ReturnsAsync(transactions);

        var result = await _controller.GetAll(null, "Dining", null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.SearchTransactionsAsync("Dining", null, null), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var tx = new Transaction { Id = 1, Description = "Test", Amount = 100m };
        _mockService.Setup(s => s.GetTransactionByIdAsync(1)).ReturnsAsync(tx);

        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_Returns404ProblemDetails_WhenNotFound()
    {
        _mockService.Setup(s => s.GetTransactionByIdAsync(999)).ReturnsAsync((Transaction?)null);

        var result = await _controller.GetById(999);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}
