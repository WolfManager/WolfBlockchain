using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Service pentru generare și validare JWT tokens - PRODUCTION GRADE
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Genereaza JWT token cu refresh token</summary>
    JwtTokenResponse GenerateToken(string userId, string address, string role);
    
    /// <summary>Valideaza JWT token</summary>
    ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>Genereaza refresh token</summary>
    string GenerateRefreshToken();

    /// <summary>Revokes a refresh token (invalidates it)</summary>
    Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);

    /// <summary>Validates if refresh token is still valid</summary>
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
}

/// <summary>
/// Response cu access token și refresh token
/// </summary>
public class JwtTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Implementare JWT Token Service cu refresh token management
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly string _jwtSecret;
    private readonly int _jwtExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly HashSet<string> _revokedTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _tokenLock = new();

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _jwtSecret = _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT:Secret not configured");
        
        _jwtExpirationMinutes = int.Parse(
            _configuration["Jwt:ExpirationMinutes"] ?? "60");

        _refreshTokenExpirationDays = int.Parse(
            _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        if (_jwtSecret.Length < 32)
            throw new InvalidOperationException("JWT:Secret must be at least 32 characters");
    }

    /// <summary>
    /// Genereaza JWT access token cu standard claims
    /// </summary>
    public JwtTokenResponse GenerateToken(string userId, string address, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("UserId and address are required");

        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, address),
                new Claim(ClaimTypes.Role, role ?? "User"),
                new Claim("address", address),
                new Claim("aud", "wolf-blockchain"),
                new Claim("iss", "wolf-blockchain-api"),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: "wolf-blockchain-api",
                audience: "wolf-blockchain",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("JWT token generated for user: {UserId}, Role: {Role}", userId, role);

            return new JwtTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtExpirationMinutes * 60,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Valideaza JWT token și extrage claims
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = "wolf-blockchain-api",
                ValidateAudience = true,
                ValidAudience = "wolf-blockchain",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed for token: {Token}", 
                token.Length > 20 ? token.Substring(0, 20) + "..." : token);
            return null;
        }
    }

    /// <summary>
    /// Genereaza refresh token (64 bytes random, base64 encoded)
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    /// <summary>
    /// Revokes a refresh token by adding it to blacklist
    /// </summary>
    public Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Task.FromResult(false);

        try
        {
            lock (_tokenLock)
            {
                var tokenKey = $"{userId}:{refreshToken}";
                _revokedTokens.Add(tokenKey);
            }

            _logger.LogInformation("Refresh token revoked for user: {UserId}", userId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user: {UserId}", userId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Validates if refresh token is still valid (not revoked)
    /// </summary>
    public Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(false);

        try
        {
            lock (_tokenLock)
            {
                var tokenKey = $"{userId}:{refreshToken}";
                var isRevoked = _revokedTokens.Contains(tokenKey);
                
                if (isRevoked)
                {
                    _logger.LogWarning("Attempt to use revoked refresh token for user: {UserId}", userId);
                }

                return Task.FromResult(!isRevoked);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user: {UserId}", userId);
            return Task.FromResult(false);
        }
    }
}
