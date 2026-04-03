using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Abstraction over UserManager to support dependency injection and testability.
/// </summary>
public interface IUserManagerService
{
    /// <summary>Registers a new user.</summary>
    BlockchainUser? RegisterUser(string address, string username, UserRole role, string password);

    /// <summary>Gets a user by blockchain address.</summary>
    BlockchainUser? GetUserByAddress(string address);

    /// <summary>Authenticates a user by address and password.</summary>
    BlockchainUser? AuthenticateUser(string address, string password);

    /// <summary>Changes the password for a user.</summary>
    bool ChangePassword(string address, string oldPassword, string newPassword);

    /// <summary>Checks whether a user has the specified permission.</summary>
    bool HasPermission(string address, Permission permission);

    /// <summary>Grants a permission to a user.</summary>
    bool GrantPermission(string address, Permission permission);

    /// <summary>Revokes a permission from a user.</summary>
    bool RevokePermission(string address, Permission permission);

    /// <summary>Deactivates a user account.</summary>
    bool DeactivateUser(string address);

    /// <summary>Activates a user account.</summary>
    bool ActivateUser(string address);

    /// <summary>Returns all registered users.</summary>
    List<BlockchainUser> GetAllUsers();
}
