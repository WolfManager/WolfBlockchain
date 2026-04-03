using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WolfBlockchain.API.Services;
using WolfBlockchain.Core;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly IUserManagerService _userManagerService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        IUserManagerService userManagerService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<SecurityController> logger)
    {
        _userManagerService = userManagerService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    private bool SingleAdminMode => _configuration.GetValue<bool>("Security:SingleAdminMode", true);
    private string OwnerAddress => _configuration["Security:OwnerAddress"] ?? "WOLFADMIN";
    private string BootstrapToken => _configuration["Security:BootstrapToken"] ?? string.Empty;
    private int MaxFailedLoginAttempts => _configuration.GetValue<int>("Security:MaxFailedLoginAttempts", 5);
    private int LoginLockoutMinutes => _configuration.GetValue<int>("Security:LoginLockoutMinutes", 30);

    /// <summary>Strips newlines and control characters to prevent log-injection attacks.</summary>
    private static string SanitizeForLog(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(value, @"[\r\n\t\x00-\x1f\x7f]", "_");

    /// <summary>
    /// Inregistrarea publică este dezactivată în single-admin mode.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult RegisterUser([FromBody] RegisterUserRequest request)
    {
        if (SingleAdminMode)
            return StatusCode(StatusCodes.Status403Forbidden, "Public registration is disabled in single-admin mode.");

        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var user = _userManagerService.RegisterUser(
            request.Address,
            request.Username,
            request.Role,
            request.Password
        );

        if (user == null)
            return BadRequest("Registration failed. Address may already exist.");

        return Ok(new
        {
            success = true,
            userId = user.UserId,
            username = user.Username,
            role = user.Role.ToString(),
            address = user.Address
        });
    }

    /// <summary>
    /// Bootstrap owner admin (one-time). Requires X-Bootstrap-Token header.
    /// </summary>
    [HttpPost("bootstrap-owner")]
    [AllowAnonymous]
    public IActionResult BootstrapOwner([FromBody] BootstrapOwnerRequest request)
    {
        if (!SingleAdminMode)
            return BadRequest("Single-admin mode is disabled.");

        if (string.IsNullOrWhiteSpace(BootstrapToken))
            return StatusCode(StatusCodes.Status500InternalServerError, "Bootstrap token is not configured.");

        if (!Request.Headers.TryGetValue("X-Bootstrap-Token", out var provided) || provided != BootstrapToken)
            return Unauthorized("Invalid bootstrap token.");

        if (!string.Equals(request.Address, OwnerAddress, StringComparison.Ordinal))
            return BadRequest("Bootstrap address must match configured owner address.");

        var owner = _userManagerService.BootstrapOwner(request.Address, request.Username, request.Password);
        if (owner == null)
            return Conflict("Owner already initialized.");

        _logger.LogInformation("Owner bootstrap completed for address: {OwnerAddress}", SanitizeForLog(request.Address));
        Log.ForContext("AuditType", "Security")
           .Information("SECURITY_AUDIT OwnerBootstrap Success Address={Address}", SanitizeForLog(request.Address));

        return Ok(new
        {
            success = true,
            message = "Owner initialized successfully.",
            userId = owner.UserId,
            address = owner.Address,
            role = owner.Role.ToString()
        });
    }

    /// <summary>
    /// Autentifică owner-ul (single-admin mode).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (SingleAdminMode)
        {
            if (!string.Equals(request.Address, OwnerAddress, StringComparison.Ordinal))
            {
                Log.ForContext("AuditType", "Security")
                   .Warning("SECURITY_AUDIT LoginDenied NonOwnerAddress={Address}", SanitizeForLog(request.Address));
                return Unauthorized("Invalid credentials");
            }

            if (_userManagerService.IsLoginLocked(out var lockedUntil))
            {
                Log.ForContext("AuditType", "Security")
                   .Warning("SECURITY_AUDIT LoginLocked Address={Address} LockedUntil={LockedUntil}", SanitizeForLog(request.Address), lockedUntil!.Value);

                return StatusCode(StatusCodes.Status423Locked, new
                {
                    success = false,
                    message = "Login temporarily locked due to repeated failed attempts.",
                    lockedUntilUtc = lockedUntil.Value
                });
            }
        }

        var user = _userManagerService.AuthenticateUser(request.Address, request.Password);
        if (user == null)
        {
            if (SingleAdminMode)
            {
                _userManagerService.RecordFailedLogin(MaxFailedLoginAttempts, LoginLockoutMinutes);

                Log.ForContext("AuditType", "Security")
                   .Warning("SECURITY_AUDIT LoginFailed Address={Address}", SanitizeForLog(request.Address));

                if (_userManagerService.IsLoginLocked(out _))
                {
                    _logger.LogWarning("Owner login locked for {Minutes} minutes.", LoginLockoutMinutes);
                    Log.ForContext("AuditType", "Security")
                       .Warning("SECURITY_AUDIT LockoutApplied Address={Address} Minutes={Minutes}", SanitizeForLog(request.Address), LoginLockoutMinutes);
                }
            }

            return Unauthorized("Invalid credentials");
        }

        if (SingleAdminMode && user.Role != UserRole.Admin)
        {
            Log.ForContext("AuditType", "Security")
               .Warning("SECURITY_AUDIT LoginDenied NonAdminRole Address={Address} Role={Role}", SanitizeForLog(user.Address), user.Role.ToString());
            return Unauthorized("Only owner admin account is allowed.");
        }

        if (SingleAdminMode)
            _userManagerService.ResetLoginAttempts();

        Log.ForContext("AuditType", "Security")
           .Information("SECURITY_AUDIT LoginSuccess Address={Address} UserId={UserId}", SanitizeForLog(user.Address), SanitizeForLog(user.UserId));

        var tokenResponse = _jwtTokenService.GenerateToken(user.UserId, user.Address, user.Role.ToString());

        return Ok(new
        {
            success = true,
            accessToken = tokenResponse.AccessToken,
            refreshToken = tokenResponse.RefreshToken,
            expiresIn = tokenResponse.ExpiresIn,
            tokenType = tokenResponse.TokenType,
            userId = user.UserId,
            username = user.Username,
            role = user.Role.ToString(),
            address = user.Address
        });
    }

    /// <summary>
    /// Obtine informatiile utilizatorului.
    /// </summary>
    [HttpGet("user/{address}")]
    public IActionResult GetUser(string address)
    {
        if (SingleAdminMode && !string.Equals(address, OwnerAddress, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, "Only owner account is accessible in single-admin mode.");

        var user = _userManagerService.GetUserByAddress(address);
        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            userId = user.UserId,
            username = user.Username,
            address = user.Address,
            role = user.Role.ToString(),
            permissions = user.Permissions.ToString(),
            isActive = user.IsActive,
            twoFactorEnabled = user.TwoFactorEnabled,
            createdAt = user.CreatedAt
        });
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (SingleAdminMode && !string.Equals(request.Address, OwnerAddress, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, "Only owner password can be changed.");

        var success = _userManagerService.ChangePassword(request.Address, request.OldPassword, request.NewPassword);
        if (!success)
            return Unauthorized("Failed to change password. Check old password.");

        Log.ForContext("AuditType", "Security")
           .Information("SECURITY_AUDIT PasswordChanged Address={Address}", SanitizeForLog(request.Address));

        return Ok(new { success = true, message = "Password changed successfully" });
    }

    [HttpGet("has-permission/{address}/{permission}")]
    public IActionResult HasPermission(string address, string permission)
    {
        if (SingleAdminMode && !string.Equals(address, OwnerAddress, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, "Only owner permissions can be queried.");

        if (!Enum.TryParse<Permission>(permission, out var perm))
            return BadRequest("Invalid permission");

        var hasPermission = _userManagerService.HasPermission(address, perm);

        return Ok(new
        {
            address,
            permission,
            hasPermission
        });
    }

    [HttpPost("grant-permission")]
    public IActionResult GrantPermission([FromBody] GrantPermissionRequest request)
    {
        if (SingleAdminMode)
            return StatusCode(StatusCodes.Status403Forbidden, "Permission management is disabled in single-admin mode.");

        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (!Enum.TryParse<Permission>(request.Permission, out var perm))
            return BadRequest("Invalid permission");

        var success = _userManagerService.GrantPermission(request.Address, perm);
        if (!success)
            return BadRequest("Failed to grant permission");

        return Ok(new { success = true, message = "Permission granted" });
    }

    [HttpPost("revoke-permission")]
    public IActionResult RevokePermission([FromBody] RevokePermissionRequest request)
    {
        if (SingleAdminMode)
            return StatusCode(StatusCodes.Status403Forbidden, "Permission management is disabled in single-admin mode.");

        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (!Enum.TryParse<Permission>(request.Permission, out var perm))
            return BadRequest("Invalid permission");

        var success = _userManagerService.RevokePermission(request.Address, perm);
        if (!success)
            return BadRequest("Failed to revoke permission");

        return Ok(new { success = true, message = "Permission revoked" });
    }

    [HttpPost("deactivate")]
    public IActionResult DeactivateUser([FromBody] DeactivateUserRequest request)
    {
        if (SingleAdminMode)
            return StatusCode(StatusCodes.Status403Forbidden, "User deactivation is disabled in single-admin mode.");

        var success = _userManagerService.DeactivateUser(request.Address);
        if (!success)
            return BadRequest("Failed to deactivate user");

        return Ok(new { success = true, message = "User deactivated" });
    }

    [HttpPost("activate")]
    public IActionResult ActivateUser([FromBody] ActivateUserRequest request)
    {
        if (SingleAdminMode)
            return StatusCode(StatusCodes.Status403Forbidden, "User activation is disabled in single-admin mode.");

        var success = _userManagerService.ActivateUser(request.Address);
        if (!success)
            return BadRequest("Failed to activate user");

        return Ok(new { success = true, message = "User activated" });
    }

    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        var users = _userManagerService.GetAllUsers();

        if (SingleAdminMode)
        {
            var owner = users.FirstOrDefault(u => string.Equals(u.Address, OwnerAddress, StringComparison.Ordinal));
            if (owner == null)
                return Ok(Array.Empty<object>());

            return Ok(new[]
            {
                new
                {
                    userId = owner.UserId,
                    username = owner.Username,
                    address = owner.Address,
                    role = owner.Role.ToString(),
                    isActive = owner.IsActive,
                    createdAt = owner.CreatedAt
                }
            });
        }

        var result = users.Select(u => new
        {
            userId = u.UserId,
            username = u.Username,
            address = u.Address,
            role = u.Role.ToString(),
            isActive = u.IsActive,
            createdAt = u.CreatedAt
        });

        return Ok(result);
    }

    [HttpPost("validate-token")]
    [AllowAnonymous]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var principal = _jwtTokenService.ValidateToken(request.Token);
        if (principal == null)
        {
            Log.ForContext("AuditType", "Security")
               .Warning("SECURITY_AUDIT TokenValidationFailed Reason=InvalidToken");
            return Ok(new { isValid = false });
        }

        var address = principal.Claims.FirstOrDefault(c => c.Type == "address")?.Value;
        var userId = principal.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var exp = principal.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

        if (SingleAdminMode && !string.Equals(address, OwnerAddress, StringComparison.Ordinal))
        {
            Log.ForContext("AuditType", "Security")
               .Warning("SECURITY_AUDIT TokenValidationDenied NonOwnerAddress={Address}", SanitizeForLog(address));
            return Ok(new { isValid = false });
        }

        DateTime? expiration = null;
        if (long.TryParse(exp, out var unixExp))
            expiration = DateTimeOffset.FromUnixTimeSeconds(unixExp).UtcDateTime;

        Log.ForContext("AuditType", "Security")
           .Information("SECURITY_AUDIT TokenValidationSuccess Address={Address} UserId={UserId}", SanitizeForLog(address), SanitizeForLog(userId));

        return Ok(new
        {
            isValid = true,
            address,
            userId,
            expiration
        });
    }

    [HttpGet("generate-password")]
    public IActionResult GeneratePassword([FromQuery] int length = 16)
    {
        if (length < 8 || length > 64)
            return BadRequest("Password length must be between 8 and 64");

        var password = SecurityUtils.GenerateSecurePassword(length);
        return Ok(new { password });
    }
}

public class BootstrapOwnerRequest
{
    public string Address { get; set; } = "";
    public string Username { get; set; } = "owner";
    public string Password { get; set; } = "";
}

public class RegisterUserRequest
{
    public string Address { get; set; } = "";
    public string Username { get; set; } = "";
    public UserRole Role { get; set; }
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Address { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string Address { get; set; } = "";
    public string OldPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class GrantPermissionRequest
{
    public string Address { get; set; } = "";
    public string Permission { get; set; } = "";
}

public class RevokePermissionRequest
{
    public string Address { get; set; } = "";
    public string Permission { get; set; } = "";
}

public class DeactivateUserRequest
{
    public string Address { get; set; } = "";
}

public class ActivateUserRequest
{
    public string Address { get; set; } = "";
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = "";
}
