namespace WolfBlockchain.Core;

/// <summary>
/// Rolurile disponibile pe Wolf Blockchain
/// </summary>
public enum UserRole
{
    /// <summary>Admin - control total, doar proprietar</summary>
    Admin = 0,
    
    /// <summary>Validator - poate valida blocuri si tranzactii</summary>
    Validator = 1,
    
    /// <summary>User - utilizator standard cu drepturi de tranzactie</summary>
    User = 2,
    
    /// <summary>ReadOnly - doar citire, fara drepturi de modificare</summary>
    ReadOnly = 3
}

/// <summary>
/// Permisiuni disponibile
/// </summary>
[Flags]
public enum Permission
{
    /// <summary>Fara permisiuni</summary>
    None = 0,
    
    /// <summary>Citire date</summary>
    Read = 1,
    
    /// <summary>Creare tranzactii</summary>
    CreateTransaction = 2,
    
    /// <summary>Validare blocuri</summary>
    ValidateBlock = 4,
    
    /// <summary>Crearea de tokeni</summary>
    CreateToken = 8,
    
    /// <summary>Administrare sistem</summary>
    AdminControl = 16,
    
    /// <summary>Mint/Burn tokeni</summary>
    MintBurn = 32,
    
    /// <summary>Gestionare utilizatori</summary>
    ManageUsers = 64,
    
    /// <summary>Tote permisiunile</summary>
    All = Read | CreateTransaction | ValidateBlock | CreateToken | AdminControl | MintBurn | ManageUsers
}

/// <summary>
/// Utilizator pe Wolf Blockchain cu rol si permisiuni
/// </summary>
public class BlockchainUser
{
    /// <summary>ID unic al utilizatorului</summary>
    public string UserId { get; set; }
    
    /// <summary>Adresa wallet</summary>
    public string Address { get; set; }
    
    /// <summary>Nume utilizator</summary>
    public string Username { get; set; }
    
    /// <summary>Rolul utilizatorului</summary>
    public UserRole Role { get; set; }
    
    /// <summary>Permisiunile personalizate</summary>
    public Permission Permissions { get; set; }
    
    /// <summary>Data crearii contului</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>Daca este activ</summary>
    public bool IsActive { get; set; }
    
    /// <summary>Daca are 2FA enabled</summary>
    public bool TwoFactorEnabled { get; set; }
    
    /// <summary>Hash parola (doar pentru reference, nu se stocheaza parola plain)</summary>
    public string PasswordHash { get; set; }

    public BlockchainUser(string address, string username, UserRole role)
    {
        UserId = Guid.NewGuid().ToString();
        Address = address;
        Username = username;
        Role = role;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        TwoFactorEnabled = false;
        PasswordHash = string.Empty;
        
        // Seteaza permisiuni bazate pe rol
        Permissions = GetDefaultPermissionsForRole(role);
    }

    /// <summary>Obtine permisiuni default pentru rol</summary>
    public static Permission GetDefaultPermissionsForRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => Permission.All,
            UserRole.Validator => Permission.Read | Permission.ValidateBlock | Permission.CreateTransaction,
            UserRole.User => Permission.Read | Permission.CreateTransaction,
            UserRole.ReadOnly => Permission.Read,
            _ => Permission.None
        };
    }

    /// <summary>Verifica daca utilizatorul are o anumita permisiune</summary>
    public bool HasPermission(Permission permission)
    {
        return (Permissions & permission) == permission;
    }

    /// <summary>Adauga permisiune</summary>
    public void GrantPermission(Permission permission)
    {
        Permissions |= permission;
    }

    /// <summary>Sterge permisiune</summary>
    public void RevokePermission(Permission permission)
    {
        Permissions &= ~permission;
    }
}
