using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Wraps <see cref="UserManager"/> as a singleton DI service,
/// initialising it with the owner address from configuration.
/// </summary>
public class UserManagerService : IUserManagerService
{
    private readonly UserManager _userManager;

    public UserManagerService(IConfiguration configuration)
    {
        var ownerAddress = configuration["Security:OwnerAddress"];
        if (string.IsNullOrWhiteSpace(ownerAddress))
        {
            Console.Error.WriteLine(
                "WARNING: Security:OwnerAddress is not configured. Falling back to default 'WOLFADMIN'. " +
                "Set Security:OwnerAddress in configuration for production environments.");
            ownerAddress = "WOLFADMIN";
        }
        _userManager = new UserManager(ownerAddress);
    }

    public BlockchainUser? RegisterUser(string address, string username, UserRole role, string password)
        => _userManager.RegisterUser(address, username, role, password);

    public BlockchainUser? GetUserByAddress(string address)
        => _userManager.GetUserByAddress(address);

    public BlockchainUser? AuthenticateUser(string address, string password)
        => _userManager.AuthenticateUser(address, password);

    public bool ChangePassword(string address, string oldPassword, string newPassword)
        => _userManager.ChangePassword(address, oldPassword, newPassword);

    public bool HasPermission(string address, Permission permission)
        => _userManager.HasPermission(address, permission);

    public bool GrantPermission(string address, Permission permission)
        => _userManager.GrantPermission(address, permission);

    public bool RevokePermission(string address, Permission permission)
        => _userManager.RevokePermission(address, permission);

    public bool DeactivateUser(string address)
        => _userManager.DeactivateUser(address);

    public bool ActivateUser(string address)
        => _userManager.ActivateUser(address);

    public List<BlockchainUser> GetAllUsers()
        => _userManager.GetAllUsers();
}
