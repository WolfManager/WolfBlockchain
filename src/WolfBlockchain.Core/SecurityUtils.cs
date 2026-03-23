using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WolfBlockchain.Core;

/// <summary>
/// Utilitar pentru encriptare si securitate
/// </summary>
public static class SecurityUtils
{
    private const int DefaultPbkdf2Iterations = 310000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MinTokenSecretLength = 32;
    private const int MaxTokenLength = 4096;

    /// <summary>Genereaza hash SHA256 pentru o string</summary>
    public static string HashSHA256(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>Genereaza hash PBKDF2 pentru parole (mai sigur decat SHA256)</summary>
    public static string HashPassword(string password, int iterations = DefaultPbkdf2Iterations)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        if (iterations < 100000)
            throw new ArgumentException("Iterations must be at least 100000 for security");

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

        // Format: v1$iterations$base64Salt$base64Hash
        return $"v1${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifica daca o parola se potriveste cu hash-ul</summary>
    public static bool VerifyPassword(string password, string hash, int legacyIterations = 10000)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            // New format: v1$iterations$base64Salt$base64Hash
            if (hash.StartsWith("v1$", StringComparison.Ordinal))
            {
                var parts = hash.Split('$');
                if (parts.Length != 4)
                    return false;

                if (!int.TryParse(parts[1], out var iterations) || iterations < 100000)
                    return false;

                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != SaltSize || expectedHash.Length != HashSize)
                    return false;

                var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
            }

            // Legacy format support: Base64(16 salt + 20 hash)
            var hashWithSalt = Convert.FromBase64String(hash);
            if (hashWithSalt.Length != 36)
                return false;

            var saltLegacy = hashWithSalt[..16];
            var expectedLegacyHash = hashWithSalt[16..36];

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltLegacy, legacyIterations, HashAlgorithmName.SHA256);
            var computedLegacyHash = pbkdf2.GetBytes(20);

            return CryptographicOperations.FixedTimeEquals(expectedLegacyHash, computedLegacyHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Genereaza token semnat (legacy-compatible)</summary>
    public static string GenerateToken(string address, string userId, DateTime expiration)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        var payload = $"{address}|{userId}|{expiration:O}";
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        var secret = GetTokenSecret();
        if (!string.IsNullOrEmpty(secret))
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64)));
            return $"{payloadBase64}.{signature}";
        }

        if (IsLegacyTokenModeEnabled())
        {
            var legacyHash = HashSHA256(payload);
            return $"{payloadBase64}.{legacyHash}";
        }

        throw new InvalidOperationException("WOLF_TOKEN_SECRET must be configured with at least 32 characters.");
    }

    /// <summary>Valideaza si extrage date din token</summary>
    public static (bool isValid, string? address, string? userId, DateTime? expiration) ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
            return (false, null, null, null);

        try
        {
            var parts = token.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return (false, null, null, null);

            var payloadBase64 = parts[0];
            var signatureOrHash = parts[1];

            var payloadBytes = Convert.FromBase64String(payloadBase64);
            var payload = Encoding.UTF8.GetString(payloadBytes);

            var secret = GetTokenSecret();
            if (!string.IsNullOrEmpty(secret))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var expectedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
                var providedSignatureBytes = Convert.FromBase64String(signatureOrHash);

                if (!CryptographicOperations.FixedTimeEquals(providedSignatureBytes, expectedSignatureBytes))
                    return (false, null, null, null);
            }
            else
            {
                if (!IsLegacyTokenModeEnabled())
                    return (false, null, null, null);

                var expectedHash = HashSHA256(payload);
                if (!string.Equals(expectedHash, signatureOrHash, StringComparison.Ordinal))
                    return (false, null, null, null);
            }

            var payloadParts = payload.Split('|', 3, StringSplitOptions.None);
            if (payloadParts.Length != 3)
                return (false, null, null, null);

            var address = payloadParts[0];
            var userId = payloadParts[1];
            if (!DateTime.TryParse(payloadParts[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiration))
                return (false, null, null, null);

            if (DateTime.UtcNow > expiration.ToUniversalTime())
                return (false, null, null, null);

            return (true, address, userId, expiration);
        }
        catch (FormatException)
        {
            return (false, null, null, null);
        }
        catch (InvalidOperationException)
        {
            return (false, null, null, null);
        }
    }

    /// <summary>Genereaza o parola aleatoare sigura</summary>
    public static string GenerateSecurePassword(int length = 16)
    {
        if (length < 8 || length > 128)
            throw new ArgumentException("Password length must be between 8 and 128");

        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        var result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            var idx = RandomNumberGenerator.GetInt32(validChars.Length);
            result.Append(validChars[idx]);
        }

        return result.ToString();
    }

    /// <summary>Genereaza OTP (One-Time Password) pentru 2FA</summary>
    public static string GenerateOTP(int length = 6)
    {
        if (length < 4 || length > 12)
            throw new ArgumentException("OTP length must be between 4 and 12");

        var otp = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            otp.Append(RandomNumberGenerator.GetInt32(10));
        }

        return otp.ToString();
    }

    /// <summary>Encripteaza string cu AES-256 (necesita key)</summary>
    public static string EncryptAES256(string plainText, byte[] key, byte[]? iv = null)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be empty");

        if (key is null || key.Length != 32)
            throw new ArgumentException("Invalid key length. Must be 32 bytes for AES-256");

        if (iv is not null && iv.Length != 16)
            throw new ArgumentException("Invalid IV length. Must be 16 bytes");

        using var aes = Aes.Create();
        aes.Key = key;

        if (iv is not null)
            aes.IV = iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        ms.Write(aes.IV, 0, aes.IV.Length);

        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>Decripteaza string cu AES-256</summary>
    public static string DecryptAES256(string cipherText, byte[] key)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be empty");

        if (key is null || key.Length != 32)
            throw new ArgumentException("Invalid key length");

        using var aes = Aes.Create();
        aes.Key = key;

        var buffer = Convert.FromBase64String(cipherText);
        if (buffer.Length < 16)
            throw new ArgumentException("Invalid cipher text payload");

        var iv = new byte[aes.IV.Length];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    private static string? GetTokenSecret()
    {
        var secret = Environment.GetEnvironmentVariable("WOLF_TOKEN_SECRET");
        return string.IsNullOrWhiteSpace(secret) || secret.Length < MinTokenSecretLength
            ? null
            : secret;
    }

    private static bool IsLegacyTokenModeEnabled()
    {
        var legacyMode = Environment.GetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN");
        return string.Equals(legacyMode, "1", StringComparison.Ordinal)
               || string.Equals(legacyMode, "true", StringComparison.OrdinalIgnoreCase);
    }
}
