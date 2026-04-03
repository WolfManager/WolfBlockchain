using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Implementare a IUserManagerService - wrapper peste UserManager din WolfBlockchain.Core.
/// Inregistrat ca Singleton pentru a pastra starea lockout-ului de login intre request-uri.
/// </summary>
public class UserManagerService : IUserManagerService
{
    private readonly UserManager _userManager;

    private readonly object _bootstrapLock = new();
    private readonly object _lockoutLock = new();
    private int _failedLoginAttempts;
    private DateTime? _lockedUntilUtc;

    public UserManagerService(IConfiguration configuration)
    {
        var ownerAddress = configuration["Security:OwnerAddress"] ?? "WOLFADMIN";
        _userManager = new UserManager(ownerAddress);
    }

    /// <inheritdoc/>
    public BlockchainUser? RegisterUser(string address, string username, UserRole role, string password)
        => _userManager.RegisterUser(address, username, role, password);

    /// <inheritdoc/>
    public BlockchainUser? AuthenticateUser(string address, string password)
        => _userManager.AuthenticateUser(address, password);

    /// <inheritdoc/>
    public BlockchainUser? GetUserByAddress(string address)
        => _userManager.GetUserByAddress(address);

    /// <inheritdoc/>
    public BlockchainUser? GetUserById(string userId)
        => _userManager.GetUserById(userId);

    /// <inheritdoc/>
    public bool HasPermission(string address, Permission permission)
        => _userManager.HasPermission(address, permission);

    /// <inheritdoc/>
    public bool GrantPermission(string address, Permission permission)
        => _userManager.GrantPermission(address, permission);

    /// <inheritdoc/>
    public bool RevokePermission(string address, Permission permission)
        => _userManager.RevokePermission(address, permission);

    /// <inheritdoc/>
    public bool DeactivateUser(string address)
        => _userManager.DeactivateUser(address);

    /// <inheritdoc/>
    public bool ActivateUser(string address)
        => _userManager.ActivateUser(address);

    /// <inheritdoc/>
    public bool ChangePassword(string address, string oldPassword, string newPassword)
        => _userManager.ChangePassword(address, oldPassword, newPassword);

    /// <inheritdoc/>
    public List<BlockchainUser> GetAllUsers()
        => _userManager.GetAllUsers();

    /// <inheritdoc/>
    public List<BlockchainUser> GetUsersByRole(UserRole role)
        => _userManager.GetUsersByRole(role);

    /// <inheritdoc/>
    public bool UpdateUserRole(string address, UserRole newRole)
        => _userManager.UpdateUserRole(address, newRole);

    /// <inheritdoc/>
    public BlockchainUser? BootstrapOwner(string address, string username, string password)
    {
        lock (_bootstrapLock)
        {
            if (_userManager.GetAllUsers().Count > 0)
                return null;

            return _userManager.RegisterUser(address, username, UserRole.Admin, password);
        }
    }

    // ─── Login lockout tracking ───────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsLoginLocked(out DateTime? lockedUntilUtc)
    {
        lock (_lockoutLock)
        {
            lockedUntilUtc = _lockedUntilUtc;
            return _lockedUntilUtc.HasValue && _lockedUntilUtc.Value > DateTime.UtcNow;
        }
    }

    /// <inheritdoc/>
    public void RecordFailedLogin(int maxAttempts, int lockoutMinutes)
    {
        lock (_lockoutLock)
        {
            _failedLoginAttempts++;
            if (_failedLoginAttempts >= maxAttempts)
                _lockedUntilUtc = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    /// <inheritdoc/>
    public void ResetLoginAttempts()
    {
        lock (_lockoutLock)
        {
            _failedLoginAttempts = 0;
            _lockedUntilUtc = null;
        }
    }
}
