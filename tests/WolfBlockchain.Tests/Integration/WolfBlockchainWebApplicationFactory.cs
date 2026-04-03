using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using Moq;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Repositories;

namespace WolfBlockchain.Tests.Integration;

/// <summary>
/// In-process WebApplicationFactory for integration tests.
/// Provides a known JWT secret and replaces external dependencies with stubs/mocks.
/// </summary>
public class WolfBlockchainWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>The test JWT signing secret — must be ≥32 characters.</summary>
    public const string TestJwtSecret = "wolf-blockchain-test-secret-key-xYz!!42";

    public WolfBlockchainWebApplicationFactory()
    {
        // Set environment variables before the in-process server starts so that
        // Program.cs can read them during WebApplication.CreateBuilder(args).
        // Double-underscore maps to the ':' hierarchy separator in .NET config.
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Security__SingleAdminMode", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use "Development" so AllowedOrigins validation is skipped and HTTPS
        // redirect is not enforced on the test server.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Replace IUnitOfWork with a no-op mock to avoid EF Core SQL Server
            // dependency. The AdminDashboardController does not call the unit of
            // work in any of its current endpoint implementations.
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork>(_ => new Mock<IUnitOfWork>().Object);

            // Register IDistributedCache (in-memory) — required by CacheService.
            services.AddDistributedMemoryCache();

            // Register a no-op IJSRuntime stub for Blazor services (ClientAuthService)
            // that are registered in DI but not invoked by API controller tests.
            services.RemoveAll<IJSRuntime>();
            services.AddSingleton<IJSRuntime>(new NoOpJSRuntime());

            // Replace RpcFailoverService with a stub that always reports healthy.
            services.RemoveAll<IRpcFailoverService>();
            services.AddSingleton<IRpcFailoverService>(new StubRpcFailoverService());
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("Jwt__Secret", null);
            Environment.SetEnvironmentVariable("Security__SingleAdminMode", null);
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Generates a valid, signed JWT token accepted by the test application.
    /// </summary>
    public static string GenerateTestJwt(string userId = "admin", string role = "Admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: "wolf-blockchain-api",
            audience: "wolf-blockchain",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Stub that always returns a healthy RPC probe result.</summary>
    private sealed class StubRpcFailoverService : IRpcFailoverService
    {
        public Task<RpcProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new RpcProbeResult(true, "stub-rpc", false, null));
    }

    /// <summary>
    /// No-op IJSRuntime stub for test environments.
    /// <para>
    /// <c>ClientAuthService</c> (which depends on <c>IJSRuntime</c>) is registered in DI for
    /// use by Blazor pages only. It is never invoked by any API controller endpoint tested here.
    /// If a test accidentally triggers a JS interop call, <c>default(TValue)</c> is returned so
    /// that the test fails at the assertion level with a clear message rather than an opaque DI
    /// exception at startup time.
    /// </para>
    /// </summary>
    private sealed class NoOpJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }
}
