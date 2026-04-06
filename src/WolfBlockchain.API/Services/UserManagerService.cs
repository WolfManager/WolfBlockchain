using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Service interface that wraps WolfBlockchain.Core.UserManager for DI/Singleton use.
/// Registered as Singleton so that login-lockout state and persisted user data are shared
/// across all requests without resorting to static fields.
/// </summary>
public interface IUserManagerService
{
    /// <summary>Register a new user</summary>
    BlockchainUser? RegisterUser(string address, string username, UserRole role, string password);

    /// <summary>Authenticate a user by address and password</summary>
    BlockchainUser? AuthenticateUser(string address, string password);

    /// <summary>Change a user's password</summary>
    bool ChangePassword(string address, string oldPassword, string newPassword);

    /// <summary>Get a user by their blockchain address</summary>
    BlockchainUser? GetUserByAddress(string address);

    /// <summary>Get all registered users</summary>
    List<BlockchainUser> GetAllUsers();

    /// <summary>Check whether a user has a specific permission</summary>
    bool HasPermission(string address, Permission permission);

    /// <summary>Grant a permission to a user</summary>
    bool GrantPermission(string address, Permission permission);

    /// <summary>Revoke a permission from a user</summary>
    bool RevokePermission(string address, Permission permission);

    /// <summary>Deactivate a user account</summary>
    bool DeactivateUser(string address);

    /// <summary>Activate a user account</summary>
    bool ActivateUser(string address);
}

/// <summary>
/// Production implementation of IUserManagerService.
/// Delegates to WolfBlockchain.Core.UserManager; the core class handles
/// file-backed persistence and thread-safe state management internally.
/// </summary>
public sealed class UserManagerService : IUserManagerService
{
    private readonly UserManager _userManager;
    private readonly ILogger<UserManagerService> _logger;

    public UserManagerService(IConfiguration configuration, ILogger<UserManagerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var ownerAddress = configuration["Security:OwnerAddress"] ?? "WOLFADMIN";
        _userManager = new UserManager(ownerAddress);
        _logger.LogInformation("UserManagerService initialised with owner address: {OwnerAddress}", ownerAddress);
    }

    /// <inheritdoc/>
    public BlockchainUser? RegisterUser(string address, string username, UserRole role, string password)
        => _userManager.RegisterUser(address, username, role, password);

    /// <inheritdoc/>
    public BlockchainUser? AuthenticateUser(string address, string password)
        => _userManager.AuthenticateUser(address, password);

    /// <inheritdoc/>
    public bool ChangePassword(string address, string oldPassword, string newPassword)
        => _userManager.ChangePassword(address, oldPassword, newPassword);

    /// <inheritdoc/>
    public BlockchainUser? GetUserByAddress(string address)
        => _userManager.GetUserByAddress(address);

    /// <inheritdoc/>
    public List<BlockchainUser> GetAllUsers()
        => _userManager.GetAllUsers();

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
}
