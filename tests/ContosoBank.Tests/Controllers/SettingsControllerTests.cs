using ContosoBank.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContosoBank.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _controller = new SettingsController(Mock.Of<ILogger<SettingsController>>());
    }

    [Fact]
    public void VerifyIdentity_ReturnsOk()
    {
        var result = _controller.VerifyIdentity();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetProfile_ReturnsOk()
    {
        var result = _controller.GetProfile();

        Assert.IsType<OkObjectResult>(result);
    }
}
