using ContosoBank.Controllers;
using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<IChaosService> _mockChaos;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _mockChaos = new Mock<IChaosService>();
        _mockChaos.Setup(c => c.GetStatus()).Returns(new ChaosStatus());
        _controller = new SettingsController(_mockChaos.Object, Mock.Of<ILogger<SettingsController>>());
    }

    [Fact]
    public async Task VerifyIdentity_ReturnsOk()
    {
        var result = await _controller.VerifyIdentity();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetProfile_ReturnsOk()
    {
        var result = _controller.GetProfile();

        Assert.IsType<OkObjectResult>(result);
    }
}
