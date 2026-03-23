using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Context;

namespace WolfBlockchain.API.Services;

/// <summary>Database connection pooling optimization service</summary>
public interface IConnectionPoolingService
{
    void ConfigureConnectionPool(DbContextOptionsBuilder optionsBuilder);
    Task<ConnectionPoolStatsDto> GetPoolStatsAsync();
}

/// <summary>Implementation with optimized pooling settings</summary>
public class ConnectionPoolingService : IConnectionPoolingService
{
    private readonly ILogger<ConnectionPoolingService> _logger;

    public ConnectionPoolingService(ILogger<ConnectionPoolingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Configure optimal connection pool settings</summary>
    public void ConfigureConnectionPool(DbContextOptionsBuilder optionsBuilder)
    {
        // ============= OPTIMAL CONNECTION POOLING SETTINGS =============
        
        var connectionString = BuildOptimizedConnectionString();
        
        optionsBuilder.UseSqlServer(
            connectionString,
            options =>
            {
                // Connection pooling with retry
                options.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                // Async operations
                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

        _logger.LogInformation("✅ Connection pooling configured with optimal settings");
    }

    /// <summary>Build connection string with pooling parameters</summary>
    private static string BuildOptimizedConnectionString()
    {
        var baseConnection = "Server=(localdb)\\mssqllocaldb;Database=WolfBlockchainDb;Trusted_Connection=true;";
        
        var poolingParams = new Dictionary<string, string>
        {
            // Connection Pool Settings
            { "Min Pool Size", "5" },          // Keep 5 connections warm
            { "Max Pool Size", "100" },        // Support 100 concurrent connections
            { "Pooling", "true" },             // Enable pooling
            { "Connection Lifetime", "300" },  // 5 minute lifetime
            { "Connection Idle Timeout", "180" }, // 3 minute idle timeout
            
            // Performance Settings
            { "Encrypt", "true" },             // Encrypted connection
            { "MultipleActiveResultSets", "true" }, // MARS enabled
            { "Connection Timeout", "30" },    // 30 second timeout
            { "Application Name", "WolfBlockchain" }, // Application tracking
            
            // Query Settings
            { "Query Timeout", "30" },         // 30 second query timeout
        };

        var connectionStringBuilder = baseConnection;
        foreach (var (key, value) in poolingParams)
        {
            connectionStringBuilder += $"{key}={value};";
        }

        return connectionStringBuilder;
    }

    /// <summary>Get connection pool statistics</summary>
    public async Task<ConnectionPoolStatsDto> GetPoolStatsAsync()
    {
        return await Task.FromResult(new ConnectionPoolStatsDto
        {
            MinPoolSize = 5,
            MaxPoolSize = 100,
            PoolingEnabled = true,
            ConnectionLifetimeSeconds = 300,
            IdleTimeoutSeconds = 180,
            QueryTimeoutSeconds = 30,
            MARSEnabled = true,
            EncryptionEnabled = true,
            OptimalSettings = true,
            RecommendedActions = GetRecommendations()
        });
    }

    private static List<string> GetRecommendations()
    {
        return new()
        {
            "Monitor actual pool usage to adjust Min/Max sizes",
            "Ensure application doesn't exceed Max Pool Size",
            "Consider connection pool prewarming on startup",
            "Implement circuit breaker for database failures",
            "Use connection pool metrics for capacity planning"
        };
    }
}

/// <summary>Connection pool statistics DTO</summary>
public record ConnectionPoolStatsDto
{
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public bool PoolingEnabled { get; set; }
    public int ConnectionLifetimeSeconds { get; set; }
    public int IdleTimeoutSeconds { get; set; }
    public int QueryTimeoutSeconds { get; set; }
    public bool MARSEnabled { get; set; }
    public bool EncryptionEnabled { get; set; }
    public bool OptimalSettings { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}
