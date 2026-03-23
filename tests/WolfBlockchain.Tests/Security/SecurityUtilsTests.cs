using Xunit;
using WolfBlockchain.Core;

namespace WolfBlockchain.Tests.Security;

/// <summary>
/// Unit tests pentru SecurityUtils - PRODUCTION GRADE
/// </summary>
public class SecurityUtilsTests
{
    // ============= SHA256 HASHING TESTS =============

    [Fact]
    public void HashSHA256_WithValidInput_ReturnsHash()
    {
        // Arrange
        var input = "test@example.com";

        // Act
        var result = SecurityUtils.HashSHA256(input);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(64, result.Length); // SHA256 = 64 hex chars
        Assert.All(result, c => Assert.True(Uri.IsHexDigit(c)));
    }

    [Fact]
    public void HashSHA256_WithEmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = SecurityUtils.HashSHA256("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void HashSHA256_WithNullInput_ReturnsEmptyString()
    {
        // Act
        var result = SecurityUtils.HashSHA256(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void HashSHA256_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "consistent-test";

        // Act
        var hash1 = SecurityUtils.HashSHA256(input);
        var hash2 = SecurityUtils.HashSHA256(input);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashSHA256_WithDifferentInput_ReturnsDifferentHash()
    {
        // Act
        var hash1 = SecurityUtils.HashSHA256("input1");
        var hash2 = SecurityUtils.HashSHA256("input2");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    // ============= PASSWORD HASHING TESTS =============

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "SecurePassword123!@#";

        // Act
        var result = SecurityUtils.HashPassword(password);

        // Assert
        Assert.NotEmpty(result);
        Assert.NotEqual(password, result); // Should not return plain password
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SecurityUtils.HashPassword(""));
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDifferentHash()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = SecurityUtils.HashPassword(password);
        var hash2 = SecurityUtils.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salt = different hash
    }

    [Fact]
    public void HashPassword_WithCustomIterations_Works()
    {
        // Arrange
        var password = "TestPassword123!";
        var iterations = 200000;

        // Act
        var result = SecurityUtils.HashPassword(password, iterations);

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public void HashPassword_WithLowIterations_ThrowsException()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SecurityUtils.HashPassword(password, 99999));
    }

    // ============= PASSWORD VERIFICATION TESTS =============

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectPassword123!@#";
        var hash = SecurityUtils.HashPassword(password);

        // Act
        var result = SecurityUtils.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "CorrectPassword123!@#";
        var hash = SecurityUtils.HashPassword(password);

        // Act
        var result = SecurityUtils.VerifyPassword("WrongPassword123!@#", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SomePassword123!";
        var hash = SecurityUtils.HashPassword(password);

        // Act
        var result = SecurityUtils.VerifyPassword("", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse()
    {
        // Act
        var result = SecurityUtils.VerifyPassword("password", "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Act
        var result = SecurityUtils.VerifyPassword("password", "invalid-hash");

        // Assert
        Assert.False(result);
    }

    // ============= SECURE PASSWORD GENERATION TESTS =============

    [Fact]
    public void GenerateSecurePassword_WithDefaultLength_Returns16Chars()
    {
        // Act
        var result = SecurityUtils.GenerateSecurePassword();

        // Assert
        Assert.Equal(16, result.Length);
    }

    [Fact]
    public void GenerateSecurePassword_WithCustomLength_ReturnsCorrectLength()
    {
        // Act
        var result = SecurityUtils.GenerateSecurePassword(32);

        // Assert
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public void GenerateSecurePassword_GeneratesDifferentPasswords()
    {
        // Act
        var pwd1 = SecurityUtils.GenerateSecurePassword();
        var pwd2 = SecurityUtils.GenerateSecurePassword();

        // Assert
        Assert.NotEqual(pwd1, pwd2);
    }

    [Fact]
    public void GenerateSecurePassword_ContainsValidCharacters()
    {
        // Arrange
        var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";

        // Act
        var result = SecurityUtils.GenerateSecurePassword();

        // Assert
        Assert.All(result, c => Assert.Contains(c, validChars));
    }

    // ============= OTP GENERATION TESTS =============

    [Fact]
    public void GenerateOTP_WithDefaultLength_Returns6Digits()
    {
        // Act
        var result = SecurityUtils.GenerateOTP();

        // Assert
        Assert.Equal(6, result.Length);
        Assert.All(result, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void GenerateOTP_WithCustomLength_ReturnsCorrectLength()
    {
        // Act
        var result = SecurityUtils.GenerateOTP(8);

        // Assert
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void GenerateOTP_ContainsOnlyDigits()
    {
        // Act
        var result = SecurityUtils.GenerateOTP();

        // Assert
        Assert.All(result, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void GenerateOTP_GeneratesDifferentCodes()
    {
        // Act
        var otp1 = SecurityUtils.GenerateOTP();
        var otp2 = SecurityUtils.GenerateOTP();

        // Assert - Note: Small chance of collision, but very unlikely
        // For statistical test, generate multiple
        var codes = Enumerable.Range(0, 100).Select(_ => SecurityUtils.GenerateOTP()).ToList();
        var distinctCodes = codes.Distinct().Count();
        
        Assert.True(distinctCodes > 90); // At least 90% unique
    }

    // ============= AES-256 ENCRYPTION TESTS =============

    [Fact]
    public void EncryptAES256_WithValidInput_ReturnsEncryptedData()
    {
        // Arrange
        var plainText = "Secret Message";
        var key = new byte[32]; // 32 bytes for AES-256
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        // Act
        var result = SecurityUtils.EncryptAES256(plainText, key);

        // Assert
        Assert.NotEmpty(result);
        Assert.NotEqual(plainText, result);
    }

    [Fact]
    public void EncryptAES256_WithInvalidKeyLength_ThrowsException()
    {
        // Arrange
        var plainText = "Secret Message";
        var shortKey = new byte[16]; // Wrong length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SecurityUtils.EncryptAES256(plainText, shortKey));
    }

    [Fact]
    public void EncryptAES256_WithEmptyPlainText_ThrowsException()
    {
        // Arrange
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SecurityUtils.EncryptAES256("", key));
    }

    // ============= AES-256 DECRYPTION TESTS =============

    [Fact]
    public void DecryptAES256_WithValidEncryption_ReturnsPlainText()
    {
        // Arrange
        var plainText = "Secret Message";
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var encrypted = SecurityUtils.EncryptAES256(plainText, key);

        // Act
        var result = SecurityUtils.DecryptAES256(encrypted, key);

        // Assert
        Assert.Equal(plainText, result);
    }

    [Fact]
    public void DecryptAES256_WithWrongKey_ReturnsGarbledText()
    {
        // Arrange
        var plainText = "Secret Message";
        var key1 = new byte[32];
        var key2 = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key1);
        System.Security.Cryptography.RandomNumberGenerator.Fill(key2);
        var encrypted = SecurityUtils.EncryptAES256(plainText, key1);

        // Act & Assert
        // Decryption with wrong key might throw or return garbage
        try
        {
            var result = SecurityUtils.DecryptAES256(encrypted, key2);
            Assert.NotEqual(plainText, result);
        }
        catch
        {
            // Expected - decryption failed
        }
    }

    [Fact]
    public void DecryptAES256_WithInvalidCipherText_ThrowsException()
    {
        // Arrange
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        // Act & Assert
        Assert.Throws<FormatException>(() => SecurityUtils.DecryptAES256("invalid-base64!", key));
    }

    // ============= ROUND-TRIP ENCRYPTION TESTS =============

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Test@123!#$")]
    [InlineData("Very Long Message " + "with lots of characters " + "to test encryption")]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalText(string plainText)
    {
        // Arrange
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);

        // Act
        var encrypted = SecurityUtils.EncryptAES256(plainText, key);
        var decrypted = SecurityUtils.DecryptAES256(encrypted, key);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    // ============= TOKEN SECURITY TESTS =============

    [Fact]
    public void GenerateToken_WithoutSecretAndLegacyDisabled_ThrowsException()
    {
        // Arrange
        var originalSecret = Environment.GetEnvironmentVariable("WOLF_TOKEN_SECRET");
        var originalLegacyMode = Environment.GetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN");

        try
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", null);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                SecurityUtils.GenerateToken("addr", "user", DateTime.UtcNow.AddMinutes(5)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", originalSecret);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", originalLegacyMode);
        }
    }

    [Fact]
    public void GenerateAndValidateToken_WithStrongSecret_ReturnsValidToken()
    {
        // Arrange
        var originalSecret = Environment.GetEnvironmentVariable("WOLF_TOKEN_SECRET");
        var originalLegacyMode = Environment.GetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN");

        try
        {
            var secret = new string('A', 32);
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", secret);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", null);

            // Act
            var token = SecurityUtils.GenerateToken("addr", "user", DateTime.UtcNow.AddMinutes(5));
            var result = SecurityUtils.ValidateToken(token);

            // Assert
            Assert.True(result.isValid);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", originalSecret);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", originalLegacyMode);
        }
    }

    [Fact]
    public void ValidateToken_WithoutSecretAndLegacyDisabled_ReturnsInvalid()
    {
        // Arrange
        var originalSecret = Environment.GetEnvironmentVariable("WOLF_TOKEN_SECRET");
        var originalLegacyMode = Environment.GetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN");

        try
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", null);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", null);
            var payload = $"addr|user|{DateTime.UtcNow.AddMinutes(5):O}";
            var payloadBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
            var legacyHash = SecurityUtils.HashSHA256(payload);
            var legacyToken = $"{payloadBase64}.{legacyHash}";

            // Act
            var result = SecurityUtils.ValidateToken(legacyToken);

            // Assert
            Assert.False(result.isValid);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", originalSecret);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", originalLegacyMode);
        }
    }

    [Fact]
    public void ValidateToken_WithLegacyEnabledAndLegacyToken_ReturnsValid()
    {
        // Arrange
        var originalSecret = Environment.GetEnvironmentVariable("WOLF_TOKEN_SECRET");
        var originalLegacyMode = Environment.GetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN");

        try
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", null);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", "1");
            var payload = $"addr|user|{DateTime.UtcNow.AddMinutes(5):O}";
            var payloadBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
            var legacyHash = SecurityUtils.HashSHA256(payload);
            var legacyToken = $"{payloadBase64}.{legacyHash}";

            // Act
            var result = SecurityUtils.ValidateToken(legacyToken);

            // Assert
            Assert.True(result.isValid);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WOLF_TOKEN_SECRET", originalSecret);
            Environment.SetEnvironmentVariable("WOLF_ALLOW_LEGACY_TOKEN", originalLegacyMode);
        }
    }
}
