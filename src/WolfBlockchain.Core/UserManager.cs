namespace WolfBlockchain.Core;

/// <summary>
/// Manager pentru utilizatori si access control
/// </summary>
public class UserManager
{
    /// <summary>Dictionar de utilizatori (UserId -> User)</summary>
    private Dictionary<string, BlockchainUser> _users;
    
    /// <summary>Dictionar de adrese la userid (Address -> UserId)</summary>
    private Dictionary<string, string> _addressToUserId;
    
    /// <summary>Adresa owner-ului blockchain (control total)</summary>
    public string OwnerAddress { get; set; }

    public UserManager(string ownerAddress)
    {
        OwnerAddress = ownerAddress;
        _users = new Dictionary<string, BlockchainUser>();
        _addressToUserId = new Dictionary<string, string>();
    }

    /// <summary>Inregistreaza un nou utilizator</summary>
    public BlockchainUser? RegisterUser(string address, string username, UserRole role, string password)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return null;

        // Verifica daca adresa exista deja
        if (_addressToUserId.ContainsKey(address))
            return null;

        var user = new BlockchainUser(address, username, role);
        user.PasswordHash = SecurityUtils.HashPassword(password);

        _users[user.UserId] = user;
        _addressToUserId[address] = user.UserId;

        Console.WriteLine($"User registered: {username} ({role})");
        return user;
    }

    /// <summary>Obtine utilizator dupa address</summary>
    public BlockchainUser? GetUserByAddress(string address)
    {
        if (!_addressToUserId.ContainsKey(address))
            return null;

        var userId = _addressToUserId[address];
        return _users.ContainsKey(userId) ? _users[userId] : null;
    }

    /// <summary>Obtine utilizator dupa UserId</summary>
    public BlockchainUser? GetUserById(string userId)
    {
        return _users.ContainsKey(userId) ? _users[userId] : null;
    }

    /// <summary>Verifica daca utilizatorul are permisiunea</summary>
    public bool HasPermission(string address, Permission permission)
    {
        var user = GetUserByAddress(address);
        if (user == null || !user.IsActive)
            return false;

        return user.HasPermission(permission);
    }

    /// <summary>Acorda permisiune utilizatorului</summary>
    public bool GrantPermission(string address, Permission permission)
    {
        // Doar owner-ul poate acorda permisiuni
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        user.GrantPermission(permission);
        Console.WriteLine($"Permission granted to {user.Username}: {permission}");
        return true;
    }

    /// <summary>Sterge permisiune utilizatorului</summary>
    public bool RevokePermission(string address, Permission permission)
    {
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        user.RevokePermission(permission);
        Console.WriteLine($"Permission revoked from {user.Username}: {permission}");
        return true;
    }

    /// <summary>Dezactiveaza utilizatorul</summary>
    public bool DeactivateUser(string address)
    {
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        user.IsActive = false;
        Console.WriteLine($"User deactivated: {user.Username}");
        return true;
    }

    /// <summary>Reactiveaza utilizatorul</summary>
    public bool ActivateUser(string address)
    {
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        user.IsActive = true;
        Console.WriteLine($"User activated: {user.Username}");
        return true;
    }

    /// <summary>Schimba parola utilizatorului</summary>
    public bool ChangePassword(string address, string oldPassword, string newPassword)
    {
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        if (!SecurityUtils.VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = SecurityUtils.HashPassword(newPassword);
        Console.WriteLine($"Password changed for: {user.Username}");
        return true;
    }

    /// <summary>Verifica credentiale utilizator</summary>
    public BlockchainUser? AuthenticateUser(string address, string password)
    {
        var user = GetUserByAddress(address);
        if (user == null || !user.IsActive)
            return null;

        if (!SecurityUtils.VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }

    /// <summary>Lista toti utilizatorii</summary>
    public List<BlockchainUser> GetAllUsers()
    {
        return _users.Values.ToList();
    }

    /// <summary>Obtine toti utilizatorii cu un anumit rol</summary>
    public List<BlockchainUser> GetUsersByRole(UserRole role)
    {
        return _users.Values.Where(u => u.Role == role).ToList();
    }

    /// <summary>Actualizeaza rolul utilizatorului (doar owner)</summary>
    public bool UpdateUserRole(string address, UserRole newRole)
    {
        var user = GetUserByAddress(address);
        if (user == null)
            return false;

        user.Role = newRole;
        user.Permissions = BlockchainUser.GetDefaultPermissionsForRole(newRole);
        Console.WriteLine($"Role updated for {user.Username}: {newRole}");
        return true;
    }
}
