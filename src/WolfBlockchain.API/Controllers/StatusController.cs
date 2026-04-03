using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Status Controller - Returns the current status of the WolfBlockchain v2.0.0 project.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class StatusController : ControllerBase
{
    private static readonly string[] Features =
    [
        "Token Management",
        "Smart Contract Deployment",
        "AI Training Monitor",
        "Real-time Dashboard (SignalR)",
        "Admin Panel",
        "Response Caching",
        "Security (JWT + RBAC)",
        "Horizontal Scaling"
    ];

    private static readonly string[] Components =
    [
        "WolfBlockchain.API",
        "WolfBlockchain.Core",
        "WolfBlockchain.Storage",
        "WolfBlockchain.Wallet",
        "WolfBlockchain.Node"
    ];

    /// <summary>
    /// Get the current status of the WolfBlockchain v2.0.0 project.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            project = "WolfBlockchain",
            version = "v2.0.0",
            status = "Production Ready",
            timestamp = DateTime.UtcNow,
            features = Features,
            components = Components
        });
    }
}
