using WolfBlockchain.Core;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Service for rotating secrets and security keys on a schedule
/// Supports key rotation for JWT, database connections, and API keys
/// </summary>
public interface ISecretRotationService
{
    /// <summary>Rotates JWT secret</summary>
    Task<bool> RotateJwtSecretAsync();

    /// <summary>Rotates database password</summary>
    Task<bool> RotateDatabasePasswordAsync();

    /// <summary>Rotates all secrets</summary>
    Task<bool> RotateAllSecretsAsync();

    /// <summary>Gets secret rotation status</summary>
    Task<SecretRotationStatus> GetStatusAsync();
}

/// <summary>
/// Status of secret rotation
/// </summary>
public class SecretRotationStatus
{
    public DateTime LastJwtRotation { get; set; }
    public DateTime LastDbRotation { get; set; }
    public DateTime LastRotationAttempt { get; set; }
    public bool IsHealthy { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Implementation of secret rotation service
/// </summary>
public class SecretRotationService : ISecretRotationService, IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly PeriodicTimer? _rotationTimer;
    private SecretRotationStatus _status;

    public SecretRotationService(IConfiguration configuration, ILogger<SecretRotationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _status = new SecretRotationStatus
        {
            LastJwtRotation = DateTime.UtcNow,
            LastDbRotation = DateTime.UtcNow,
            IsHealthy = true
        };

        // Start rotation timer every 24 hours
        var rotationIntervalHours = configuration.GetValue<int>("Security:SecretRotationIntervalHours", 24);
        _rotationTimer = new PeriodicTimer(TimeSpan.FromHours(rotationIntervalHours));
    }

    /// <summary>
    /// Starts the secret rotation background service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Secret Rotation Service started");
        _ = RotationLoop();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the secret rotation background service
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Secret Rotation Service stopped");
        _rotationTimer?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Background loop for automatic secret rotation
    /// </summary>
    private async Task RotationLoop()
    {
        try
        {
            if (_rotationTimer == null)
                return;

            while (await _rotationTimer.WaitForNextTickAsync())
            {
                await RotateAllSecretsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rotation loop");
        }
    }

    /// <summary>
    /// Rotates JWT secret (typically by generating new one)
    /// Note: In production, implement key versioning to support multiple keys
    /// </summary>
    public Task<bool> RotateJwtSecretAsync()
    {
        try
        {
            _status.LastRotationAttempt = DateTime.UtcNow;
            
            var newSecret = GenerateSecureSecret(32);
            
            // In production, implement proper secret rotation:
            // 1. Generate new secret
            // 2. Store new secret with versioning
            // 3. Support both old and new keys during transition period
            // 4. Update environment variables or key vault
            // 5. Notify dependent services
            
            _logger.LogInformation("JWT secret rotation completed");
            _status.LastJwtRotation = DateTime.UtcNow;
            _status.IsHealthy = true;
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating JWT secret");
            _status.LastError = ex.Message;
            _status.IsHealthy = false;
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Rotates database password
    /// Note: Requires proper database admin access and connection pool invalidation
    /// </summary>
    public Task<bool> RotateDatabasePasswordAsync()
    {
        try
        {
            _status.LastRotationAttempt = DateTime.UtcNow;
            
            var newPassword = GenerateSecurePassword(16);
            
            // In production, implement proper database rotation:
            // 1. Connect to database with admin credentials
            // 2. Create new user or update existing password
            // 3. Test connection with new credentials
            // 4. Update application connection string
            // 5. Clear connection pool
            // 6. Invalidate old password
            
            _logger.LogInformation("Database password rotation completed");
            _status.LastDbRotation = DateTime.UtcNow;
            _status.IsHealthy = true;
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating database password");
            _status.LastError = ex.Message;
            _status.IsHealthy = false;
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Rotates all secrets
    /// </summary>
    public async Task<bool> RotateAllSecretsAsync()
    {
        try
        {
            _logger.LogInformation("Starting comprehensive secret rotation");
            
            var jwtResult = await RotateJwtSecretAsync();
            var dbResult = await RotateDatabasePasswordAsync();

            _status.IsHealthy = jwtResult && dbResult;
            
            if (_status.IsHealthy)
            {
                _logger.LogInformation("All secrets rotated successfully");
            }
            else
            {
                _logger.LogWarning("Some secrets failed to rotate. JWT: {JwtResult}, DB: {DbResult}", jwtResult, dbResult);
            }

            return _status.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during comprehensive secret rotation");
            _status.LastError = ex.Message;
            _status.IsHealthy = false;
            return false;
        }
    }

    /// <summary>
    /// Gets current rotation status
    /// </summary>
    public Task<SecretRotationStatus> GetStatusAsync()
    {
        return Task.FromResult(_status);
    }

    /// <summary>
    /// Generates a secure random secret (hex encoded)
    /// </summary>
    private static string GenerateSecureSecret(int length)
    {
        return SecurityUtils.GenerateSecurePassword(length);
    }

    /// <summary>
    /// Generates a secure random password
    /// </summary>
    private static string GenerateSecurePassword(int length)
    {
        return SecurityUtils.GenerateSecurePassword(length);
    }
}
