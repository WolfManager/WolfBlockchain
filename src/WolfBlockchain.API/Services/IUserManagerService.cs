using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Abstraction over UserManager - facilitates DI and testability.
/// Registered as Singleton so login-lockout state is shared across requests.
/// </summary>
public interface IUserManagerService
{
    /// <summary>Inregistreaza un nou utilizator.</summary>
    BlockchainUser? RegisterUser(string address, string username, UserRole role, string password);

    /// <summary>
    /// Verifica credentialele si returneaza utilizatorul daca sunt corecte.
    /// Returneaza null daca autentificarea esueaza.
    /// </summary>
    BlockchainUser? AuthenticateUser(string address, string password);

    /// <summary>Obtine utilizator dupa adresa.</summary>
    BlockchainUser? GetUserByAddress(string address);

    /// <summary>Obtine utilizator dupa UserId.</summary>
    BlockchainUser? GetUserById(string userId);

    /// <summary>Verifica daca utilizatorul are permisiunea specificata.</summary>
    bool HasPermission(string address, Permission permission);

    /// <summary>Acorda o permisiune utilizatorului.</summary>
    bool GrantPermission(string address, Permission permission);

    /// <summary>Sterge o permisiune utilizatorului.</summary>
    bool RevokePermission(string address, Permission permission);

    /// <summary>Dezactiveaza utilizatorul.</summary>
    bool DeactivateUser(string address);

    /// <summary>Reactiveaza utilizatorul.</summary>
    bool ActivateUser(string address);

    /// <summary>Schimba parola utilizatorului.</summary>
    bool ChangePassword(string address, string oldPassword, string newPassword);

    /// <summary>Returneaza toti utilizatorii.</summary>
    List<BlockchainUser> GetAllUsers();

    /// <summary>Returneaza toti utilizatorii cu rolul specificat.</summary>
    List<BlockchainUser> GetUsersByRole(UserRole role);

    /// <summary>Actualizeaza rolul utilizatorului.</summary>
    bool UpdateUserRole(string address, UserRole newRole);

    /// <summary>
    /// Bootstrap atomics: verifica daca nu exista utilizatori, apoi creaza owner-ul cu rol Admin.
    /// Returneaza null daca owner-ul este deja initializat.
    /// </summary>
    BlockchainUser? BootstrapOwner(string address, string username, string password);

    // ─── Login lockout tracking ───────────────────────────────────────────────

    /// <summary>
    /// Verifica daca login-ul este blocat in urma prea multor incercari esuate.
    /// </summary>
    /// <param name="lockedUntilUtc">Momentul pana la care este blocat, sau null daca nu e blocat.</param>
    bool IsLoginLocked(out DateTime? lockedUntilUtc);

    /// <summary>
    /// Inregistreaza o incercare esuata de login si aplica lockout-ul daca
    /// s-a atins numarul maxim de incercari.
    /// </summary>
    void RecordFailedLogin(int maxAttempts, int lockoutMinutes);

    /// <summary>Reseteaza contorul de incercari esuate dupa un login reusit.</summary>
    void ResetLoginAttempts();
}
