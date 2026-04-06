using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using WolfBlockchain.API.Hubs;
using WolfBlockchain.API.Middleware;
using WolfBlockchain.API.Services;
using WolfBlockchain.API.Validation;
using WolfBlockchain.API.Monitoring;
using WolfBlockchain.Core;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============= CONFIGURATION =============
var jwtSecret = builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT:Secret must be configured via secure configuration (user-secrets for Development, env var Jwt__Secret or K8s Secret for non-Development).");
}

if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT:Secret must be at least 32 characters. Current length: {jwtSecret.Length}. " +
        "Generate a new secret with: [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([Guid]::NewGuid().ToString() + [Guid]::NewGuid().ToString()))");
}

var singleAdminMode = builder.Configuration.GetValue<bool>("Security:SingleAdminMode", true);
var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

if (!builder.Environment.IsDevelopment() && singleAdminMode && allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Security:AllowedOrigins must be configured when SingleAdminMode is enabled outside Development.");
}

// ============= LOGGING =============
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/wolf-blockchain-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt =>
            evt.Properties.TryGetValue("AuditType", out var auditType)
            && auditType.ToString().Contains("Security", StringComparison.OrdinalIgnoreCase))
        .WriteTo.File("logs/security-audit-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 90,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [AUDIT] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

builder.Host.UseSerilog();

// ============= SERVICES =============
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Wolf Blockchain API",
        Version = "v1",
        Description = "Production-Grade Blockchain API with Security, Validation, Rate Limiting & Performance Monitoring"
    });
});

// ============= DATABASE =============
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        connectionString = "Server=(localdb)\\mssqllocaldb;Database=WolfBlockchainDb;Trusted_Connection=true;MultipleActiveResultSets=true;";
        Log.Warning("Using development LocalDB connection string fallback.");
    }
    else
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must be configured outside Development (env var or secure config). ");
    }
}

builder.Services.AddDbContext<WolfBlockchainDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============= AUTHENTICATION =============
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = "wolf-blockchain-api",
        ValidateAudience = true,
        ValidAudience = "wolf-blockchain",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ============= DEPENDENCY INJECTION =============
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IPerformanceOptimizationService, PerformanceOptimizationService>();

// Task 4: API Optimization Services
builder.Services.AddScoped<IConnectionPoolingService, ConnectionPoolingService>();
builder.Services.AddScoped<IBatchingService, BatchingService>();
builder.Services.AddScoped<ClientAuthService>();
builder.Services.AddScoped<RealtimeUpdateService>();
builder.Services.AddScoped<AdminDashboardCacheService>();

builder.Services.Configure<RpcFailoverOptions>(options =>
{
    options.PrimaryEndpoint = builder.Configuration["RPC_PRIMARY"] ?? builder.Configuration["Blockchain:RpcPrimary"];
    options.FallbackEndpoint = builder.Configuration["RPC_FALLBACK"] ?? builder.Configuration["Blockchain:RpcFallback"];
    options.AuthToken = builder.Configuration["RPC_AUTH_TOKEN"];
    options.TimeoutSeconds = builder.Configuration.GetValue<int>("Blockchain:RpcTimeoutSeconds", 5);
    options.RetryCount = builder.Configuration.GetValue<int>("Blockchain:RpcRetryCount", 2);
    options.BackoffMs = builder.Configuration.GetValue<int>("Blockchain:RpcBackoffMs", 250);
});

builder.Services.AddHttpClient<IRpcFailoverService, RpcFailoverService>();

// ============= AI CHAT SERVICE =============
// Option A: Ollama (local LLM) — active when Ollama:BaseUrl is configured
// Option C: Mock/Stub           — active in Development or when Ollama is not configured
var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"];
if (!string.IsNullOrWhiteSpace(ollamaBaseUrl))
{
    builder.Services.AddHttpClient<IChatService, OllamaService>(client =>
    {
        client.BaseAddress = new Uri(ollamaBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Ollama:TimeoutSeconds", 60));
    });
    Log.Information("✅ Chat service configured: Ollama at {BaseUrl}", ollamaBaseUrl);
}
else
{
    builder.Services.AddSingleton<IChatService, MockChatService>();
    Log.Information("ℹ️ Chat service configured: Mock (set Ollama:BaseUrl to enable local AI)");
}

builder.Services.AddSingleton<IPerformanceMetrics, WolfBlockchain.API.Monitoring.PerformanceMetrics>();
builder.Services.AddSingleton<ISecretRotationService, SecretRotationService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ISecretRotationService>() as SecretRotationService ?? throw new InvalidOperationException());

// ============= SIGNALR (Real-time Updates) =============
builder.Services.AddSignalR();
Log.Information("✅ SignalR configured for real-time blockchain updates");

// ============= CACHING (Memory-based, Redis-ready) =============
builder.Services.AddMemoryCache();
Log.Information("✅ Caching service configured (in-memory)");
Log.Information("💡 Redis support ready: Add StackExchange.Redis NuGet package to enable");

// ============= CORS =============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWolfBlockchain", policy =>
    {
        if (singleAdminMode)
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // fail-safe in single-admin mode: no cross-origin access if not configured
                policy.WithOrigins("http://localhost")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ============= HEALTH CHECKS =============
builder.Services.AddHealthChecks();

var app = builder.Build();

// ============= MIDDLEWARE PIPELINE =============
app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<RequestSizeLimitingMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<AdminIpAllowlistMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowWolfBlockchain");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/ready", async (IConfiguration configuration, IRpcFailoverService rpcFailoverService, CancellationToken ct) =>
{
    var jwtConfigured = !string.IsNullOrWhiteSpace(configuration["Jwt:Secret"]);
    var connectionStringConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"));

    var dataDirectory = Environment.GetEnvironmentVariable("WOLF_DATA_PATH") ?? "/app/data";
    var storageReady = false;
    try
    {
        Directory.CreateDirectory(dataDirectory);
        var probePath = Path.Combine(dataDirectory, ".ready-probe");
        await File.WriteAllTextAsync(probePath, DateTime.UtcNow.ToString("O"), ct);
        File.Delete(probePath);
        storageReady = true;
    }
    catch (IOException)
    {
        storageReady = false;
    }
    catch (UnauthorizedAccessException)
    {
        storageReady = false;
    }

    RpcProbeResult rpc;
    try
    {
        using var rpcTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        rpcTimeoutCts.CancelAfter(TimeSpan.FromSeconds(1));
        rpc = await rpcFailoverService.ProbeAsync(rpcTimeoutCts.Token);
    }
    catch (OperationCanceledException)
    {
        rpc = new RpcProbeResult(false, null, false, "RPC probe timed out.");
    }

    var isReady = jwtConfigured && connectionStringConfigured && storageReady && rpc.IsHealthy;

    return isReady
        ? Results.Ok(new
        {
            status = "ready",
            rpc = new { healthy = true, activeHost = rpc.ActiveEndpointHost, fallback = rpc.UsedFallback },
            storage = "ok"
        })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();
app.MapGet("/metrics", (IPerformanceMetrics performanceMetrics) =>
{
    var stats = performanceMetrics.GetStatistics();
    var memoryMb = GC.GetTotalMemory(false) / 1024d / 1024d;

    var payload = string.Join('\n',
    [
        "# HELP wolfblockchain_requests_total Total requests processed",
        "# TYPE wolfblockchain_requests_total counter",
        $"wolfblockchain_requests_total {stats.TotalRequests}",
        "# HELP wolfblockchain_errors_total Total failed requests",
        "# TYPE wolfblockchain_errors_total counter",
        $"wolfblockchain_errors_total {stats.ErrorCount}",
        "# HELP wolfblockchain_response_time_avg_ms Average response time in milliseconds",
        "# TYPE wolfblockchain_response_time_avg_ms gauge",
        $"wolfblockchain_response_time_avg_ms {stats.AverageResponseTimeMs:F2}",
        "# HELP wolfblockchain_memory_mb Current managed memory in megabytes",
        "# TYPE wolfblockchain_memory_mb gauge",
        $"wolfblockchain_memory_mb {memoryMb:F2}"
    ]);

    return Results.Text($"{payload}\n", "text/plain; version=0.0.4");
}).AllowAnonymous();
app.MapControllers();
app.MapHub<BlockchainHub>("/blockchain-hub").AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<WolfBlockchainDbContext>();
        // NOTE: Migrations will be applied when available in WolfBlockchain.Storage
        // For now, ensure database exists without migration
        db.Database.EnsureCreated();
        Log.Information("✅ Database ensured/migrated successfully!");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️  Database initialization skipped (may be expected in local/test environments)");
    }
}

Log.Information("========================================");
Log.Information("  🐺 WOLF BLOCKCHAIN API - SINGLE ADMIN MODE");
Log.Information("  Security: JWT + HTTPS + Headers + IP Allowlist + Login Lockout");
Log.Information("  Service started. Health endpoints: /health, /ready");
Log.Information("  SignalR Real-time Hub: /blockchain-hub");
Log.Information("========================================");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}