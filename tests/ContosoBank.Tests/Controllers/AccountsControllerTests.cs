using ContosoBank.Controllers;
using ContosoBank.Models;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IAccountService> _mockService;
    private readonly Mock<IChaosService> _mockChaos;
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _mockService = new Mock<IAccountService>();
        _mockChaos = new Mock<IChaosService>();
        _mockChaos.Setup(c => c.GetStatus()).Returns(new ChaosStatus());
        _controller = new AccountsController(_mockService.Object, _mockChaos.Object, Mock.Of<ILogger<AccountsController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithAccounts()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, AccountNumber = "CHK-001", AccountName = "Checking", Balance = 1000m },
            new() { Id = 2, AccountNumber = "SAV-001", AccountName = "Savings", Balance = 5000m }
        };
        _mockService.Setup(s => s.GetAllAccountsAsync()).ReturnsAsync(accounts);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<Account>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var account = new Account { Id = 1, AccountNumber = "CHK-001", AccountName = "Test", Balance = 1000m };
        _mockService.Setup(s => s.GetAccountByIdAsync(1)).ReturnsAsync(account);

        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<Account>(ok.Value);
        Assert.Equal("CHK-001", returned.AccountNumber);
    }

    [Fact]
    public async Task GetById_Returns404ProblemDetails_WhenNotFound()
    {
        _mockService.Setup(s => s.GetAccountByIdAsync(999)).ReturnsAsync((Account?)null);

        var result = await _controller.GetById(999);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}
