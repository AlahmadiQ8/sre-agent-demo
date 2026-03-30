using Microsoft.AspNetCore.Mvc;

namespace ContosoBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ILogger<SettingsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("verify-identity")]
    public IActionResult VerifyIdentity()
    {
        // KYC verification — this endpoint exists as a chaos trigger
        // (Scenario 6: Dependency Timeout). Normal operation returns success.
        _logger.LogInformation("Identity verification (KYC) requested");

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
        // Static demo user profile — no actual user model (per spec)
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
