using ContosoBank.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IChaosService _chaosService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IChaosService chaosService, ILogger<SettingsController> logger)
    {
        _chaosService = chaosService;
        _logger = logger;
    }

    [HttpPost("verify-identity")]
    public async Task<IActionResult> VerifyIdentity()
    {
        _logger.LogInformation("Identity verification (KYC) requested");

        // Chaos Scenario 6: Dependency Timeout — simulates external KYC provider unreachable
        await _chaosService.TriggerDependencyTimeout();

        if (_chaosService.GetStatus().IsDependencyTimeoutActive)
        {
            _logger.LogError("KYC verification timed out: external identity provider unreachable");
            return Problem(
                title: "Identity verification failed",
                detail: "The identity verification service is currently unavailable. Please try again later.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }

        return Ok(new
        {
            Status = "Verified",
            VerifiedAt = DateTime.UtcNow,
            Message = "Identity verification completed successfully."
        });
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        return Ok(new
        {
            Name = "Alex Johnson",
            Email = "alex.johnson@contoso.com",
            Phone = "+1 (555) 123-4567",
            MemberSince = new DateTime(2020, 3, 15),
            NotificationsEnabled = true,
            TwoFactorEnabled = false
        });
    }
}
