using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WolfBlockchain.Api.Abstractions;

namespace WolfBlockchain.Api.PublicApi;

public static class PublicApiEndpoints
{
    public static IEndpointRouteBuilder MapWolfPublicApiV1(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/public");

        group.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        group.MapGet("/chain/status", async (IPublicApiService apiService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var context = BuildRequestContext(httpContext);
            var result = await apiService.GetChainStatusAsync(context, cancellationToken).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapGet("/chain/blocks/{blockHash}", async (string blockHash, IPublicApiService apiService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var context = BuildRequestContext(httpContext);
            var result = await apiService.GetBlockByHashAsync(blockHash, context, cancellationToken).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.NotFound(result);
        });

        group.MapPost("/tx/submit", async (HttpContext httpContext, IPublicApiService apiService, CancellationToken cancellationToken) =>
        {
            using var buffer = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

            var context = BuildRequestContext(httpContext);
            var result = await apiService.SubmitTransactionAsync(buffer.ToArray(), context, cancellationToken).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return endpoints;
    }

    private static ApiRequestContext BuildRequestContext(HttpContext context)
    {
        return new ApiRequestContext(
            context.TraceIdentifier,
            context.User?.Identity?.Name ?? "anonymous",
            context.Connection.RemoteIpAddress?.ToString());
    }
}
