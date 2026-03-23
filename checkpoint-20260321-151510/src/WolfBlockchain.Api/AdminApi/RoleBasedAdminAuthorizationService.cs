using WolfBlockchain.Api.Abstractions;

namespace WolfBlockchain.Api.AdminApi;

public sealed class RoleBasedAdminAuthorizationService : IAdminAuthorizationService
{
    public bool IsAuthorized(IReadOnlyCollection<string> roles, string requiredRole)
    {
        if (string.IsNullOrWhiteSpace(requiredRole) || roles is null || roles.Count == 0)
        {
            return false;
        }

        return roles.Any(role => string.Equals(role, requiredRole, StringComparison.OrdinalIgnoreCase));
    }
}
