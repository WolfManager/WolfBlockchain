using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WolfBlockchain.Api.Abstractions;

namespace WolfBlockchain.Api.AdminApi;

public static class AdminApiEndpoints
{
    public static IEndpointRouteBuilder MapWolfAdminApiV1(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin");

        group.MapGet("/node/status", async (HttpContext httpContext, IAdminAuthorizationService authorizationService, IAdminApiService apiService, CancellationToken cancellationToken) =>
        {
            if (!IsAdminAuthorized(httpContext, authorizationService))
            {
                return Results.Forbid();
            }

            var context = BuildRequestContext(httpContext);
            var result = await apiService.GetNodeStatusAsync(context, cancellationToken).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapGet("/consensus/status", async (HttpContext httpContext, IAdminAuthorizationService authorizationService, IAdminApiService apiService, CancellationToken cancellationToken) =>
        {
            if (!IsAdminAuthorized(httpContext, authorizationService))
            {
                return Results.Forbid();
            }

            var context = BuildRequestContext(httpContext);
            var result = await apiService.GetConsensusStatusAsync(context, cancellationToken).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return endpoints;
    }

    private static bool IsAdminAuthorized(HttpContext context, IAdminAuthorizationService authorizationService)
    {
        var header = context.Request.Headers["X-Role"].ToString();
        var roles = string.IsNullOrWhiteSpace(header)
            ? Array.Empty<string>()
            : header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return authorizationService.IsAuthorized(roles, "admin");
    }

    private static ApiRequestContext BuildRequestContext(HttpContext context)
    {
        return new ApiRequestContext(
            context.TraceIdentifier,
            context.User?.Identity?.Name ?? "admin-anonymous",
            context.Connection.RemoteIpAddress?.ToString());
    }
}
