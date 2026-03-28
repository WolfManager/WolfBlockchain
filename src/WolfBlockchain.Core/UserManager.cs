using System.Text.Json;

namespace WolfBlockchain.Core;

/// <summary>
/// Manager pentru utilizatori si access control
/// </summary>
public class UserManager
{
    private const string UsersStateFileName = "users-state.json";

    /// <summary>Dictionar de utilizatori (UserId -> User)</summary>
    private Dictionary<string, BlockchainUser> _users;

    /// <summary>Dictionar de adrese la userid (Address -> UserId)</summary>
    private Dictionary<string, string> _addressToUserId;

    /// <summary>Adresa owner-ului blockchain (control total)</summary>
    public string OwnerAddress { get; set; }

    private string UsersStateFilePath { get; }
    private readonly object _stateLock = new();
    private DateTime _lastLoadedWriteTimeUtc;

    public UserManager(string ownerAddress)
    {
        OwnerAddress = ownerAddress;
        _users = new Dictionary<string, BlockchainUser>();
        _addressToUserId = new Dictionary<string, string>();

        var dataRoot = ResolveDataRootPath();
        Directory.CreateDirectory(dataRoot);
        UsersStateFilePath = Path.Combine(dataRoot, UsersStateFileName);

        LoadState();
    }

    /// <summary>Inregistreaza un nou utilizator</summary>
    public BlockchainUser? RegisterUser(string address, string username, UserRole role, string password)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return null;

        lock (_stateLock)
        {
            EnsureFreshState();

            if (_addressToUserId.ContainsKey(address))
                return null;

            var user = new BlockchainUser(address, username, role)
            {
                PasswordHash = SecurityUtils.HashPassword(password)
            };

            _users[user.UserId] = user;
            _addressToUserId[address] = user.UserId;

            if (!PersistState())
            {
                _users.Remove(user.UserId);
                _addressToUserId.Remove(address);
                return null;
            }

            Console.WriteLine($"User registered: {username} ({role})");
            return user;
        }
    }

    /// <summary>Obtine utilizator dupa address</summary>
    public BlockchainUser? GetUserByAddress(string address)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.ContainsKey(address))
                return null;

            var userId = _addressToUserId[address];
            return _users.TryGetValue(userId, out var user) ? user : null;
        }
    }

    /// <summary>Obtine utilizator dupa UserId</summary>
    public BlockchainUser? GetUserById(string userId)
    {
        lock (_stateLock)
        {
            EnsureFreshState();
            return _users.TryGetValue(userId, out var user) ? user : null;
        }
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
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            var oldPermissions = user.Permissions;
            user.GrantPermission(permission);

            if (!PersistState())
            {
                user.Permissions = oldPermissions;
                return false;
            }

            Console.WriteLine($"Permission granted to {user.Username}: {permission}");
            return true;
        }
    }

    /// <summary>Sterge permisiune utilizatorului</summary>
    public bool RevokePermission(string address, Permission permission)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            var oldPermissions = user.Permissions;
            user.RevokePermission(permission);

            if (!PersistState())
            {
                user.Permissions = oldPermissions;
                return false;
            }

            Console.WriteLine($"Permission revoked from {user.Username}: {permission}");
            return true;
        }
    }

    /// <summary>Dezactiveaza utilizatorul</summary>
    public bool DeactivateUser(string address)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            var wasActive = user.IsActive;
            user.IsActive = false;

            if (!PersistState())
            {
                user.IsActive = wasActive;
                return false;
            }

            Console.WriteLine($"User deactivated: {user.Username}");
            return true;
        }
    }

    /// <summary>Reactiveaza utilizatorul</summary>
    public bool ActivateUser(string address)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            var wasActive = user.IsActive;
            user.IsActive = true;

            if (!PersistState())
            {
                user.IsActive = wasActive;
                return false;
            }

            Console.WriteLine($"User activated: {user.Username}");
            return true;
        }
    }

    /// <summary>Schimba parola utilizatorului</summary>
    public bool ChangePassword(string address, string oldPassword, string newPassword)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            if (!SecurityUtils.VerifyPassword(oldPassword, user.PasswordHash))
                return false;

            var oldHash = user.PasswordHash;
            user.PasswordHash = SecurityUtils.HashPassword(newPassword);

            if (!PersistState())
            {
                user.PasswordHash = oldHash;
                return false;
            }

            Console.WriteLine($"Password changed for: {user.Username}");
            return true;
        }
    }

    /// <summary>Verifica credentiale utilizator</summary>
    public BlockchainUser? AuthenticateUser(string address, string password)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return null;

            if (!user.IsActive)
                return null;

            if (!SecurityUtils.VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }
    }

    /// <summary>Lista toti utilizatorii</summary>
    public List<BlockchainUser> GetAllUsers()
    {
        lock (_stateLock)
        {
            EnsureFreshState();
            return _users.Values.ToList();
        }
    }

    /// <summary>Obtine toti utilizatorii cu un anumit rol</summary>
    public List<BlockchainUser> GetUsersByRole(UserRole role)
    {
        lock (_stateLock)
        {
            EnsureFreshState();
            return _users.Values.Where(u => u.Role == role).ToList();
        }
    }

    /// <summary>Actualizeaza rolul utilizatorului (doar owner)</summary>
    public bool UpdateUserRole(string address, UserRole newRole)
    {
        lock (_stateLock)
        {
            EnsureFreshState();

            if (!_addressToUserId.TryGetValue(address, out var userId) || !_users.TryGetValue(userId, out var user))
                return false;

            var oldRole = user.Role;
            var oldPermissions = user.Permissions;

            user.Role = newRole;
            user.Permissions = BlockchainUser.GetDefaultPermissionsForRole(newRole);

            if (!PersistState())
            {
                user.Role = oldRole;
                user.Permissions = oldPermissions;
                return false;
            }

            Console.WriteLine($"Role updated for {user.Username}: {newRole}");
            return true;
        }
    }

    private bool PersistState()
    {
        try
        {
            var snapshot = new UserManagerState
            {
                OwnerAddress = OwnerAddress,
                Users = _users.Values.Select(u => new PersistedBlockchainUser
                {
                    UserId = u.UserId,
                    Address = u.Address,
                    Username = u.Username,
                    Role = u.Role,
                    Permissions = u.Permissions,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    PasswordHash = u.PasswordHash
                }).ToList()
            };

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(UsersStateFilePath, json);
            _lastLoadedWriteTimeUtc = GetStateFileWriteTimeUtc() ?? DateTime.UtcNow;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void EnsureFreshState()
    {
        var stateWriteTimeUtc = GetStateFileWriteTimeUtc();
        if (!stateWriteTimeUtc.HasValue)
            return;

        if (_lastLoadedWriteTimeUtc >= stateWriteTimeUtc.Value)
            return;

        LoadState();
    }

    private DateTime? GetStateFileWriteTimeUtc()
    {
        try
        {
            if (!File.Exists(UsersStateFilePath))
                return null;

            return File.GetLastWriteTimeUtc(UsersStateFilePath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void LoadState()
    {
        if (!File.Exists(UsersStateFilePath))
            return;

        string json;
        try
        {
            json = File.ReadAllText(UsersStateFilePath);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to read user state from '{UsersStateFilePath}'.", ex);
        }

        UserManagerState? state;
        try
        {
            state = JsonSerializer.Deserialize<UserManagerState>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"User state file '{UsersStateFilePath}' is invalid JSON.", ex);
        }

        if (state == null)
            return;

        _users = new Dictionary<string, BlockchainUser>();
        _addressToUserId = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(state.OwnerAddress))
            OwnerAddress = state.OwnerAddress;

        foreach (var persisted in state.Users)
        {
            if (string.IsNullOrWhiteSpace(persisted.UserId)
                || string.IsNullOrWhiteSpace(persisted.Address)
                || string.IsNullOrWhiteSpace(persisted.Username))
            {
                continue;
            }

            var user = new BlockchainUser(persisted.Address, persisted.Username, persisted.Role)
            {
                UserId = persisted.UserId,
                Permissions = persisted.Permissions,
                CreatedAt = persisted.CreatedAt,
                IsActive = persisted.IsActive,
                TwoFactorEnabled = persisted.TwoFactorEnabled,
                PasswordHash = persisted.PasswordHash ?? string.Empty
            };

            _users[user.UserId] = user;
            _addressToUserId[user.Address] = user.UserId;
        }

        _lastLoadedWriteTimeUtc = GetStateFileWriteTimeUtc() ?? DateTime.UtcNow;
    }

    private static string ResolveDataRootPath()
    {
        var explicitPath = Environment.GetEnvironmentVariable("WOLF_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return explicitPath;

        const string containerPath = "/app/data";
        if (Directory.Exists(containerPath))
            return containerPath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WolfBlockchain", "data");
    }

    private sealed record UserManagerState
    {
        public string? OwnerAddress { get; init; }
        public List<PersistedBlockchainUser> Users { get; init; } = new();
    }

    private sealed record PersistedBlockchainUser
    {
        public string UserId { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public Permission Permissions { get; init; }
        public DateTime CreatedAt { get; init; }
        public bool IsActive { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public string? PasswordHash { get; init; }
    }
}
