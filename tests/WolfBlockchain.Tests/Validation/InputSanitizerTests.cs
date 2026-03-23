using Xunit;
using WolfBlockchain.API.Validation;

namespace WolfBlockchain.Tests.Validation;

/// <summary>
/// Unit tests pentru InputSanitizer - PRODUCTION GRADE
/// </summary>
public class InputSanitizerTests
{
    private readonly InputSanitizer _sanitizer = new();

    // ============= STRING SANITIZATION TESTS =============

    [Fact]
    public void SanitizeString_WithValidInput_ReturnsInput()
    {
        // Act
        var result = _sanitizer.SanitizeString("Hello World");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void SanitizeString_WithXssInput_RemovesHtmlTags()
    {
        // Act
        var result = _sanitizer.SanitizeString("<script>alert('XSS')</script>");

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Fact]
    public void SanitizeString_WithLongInput_TruncatesCorrectly()
    {
        // Arrange
        var longInput = new string('a', 600);

        // Act
        var result = _sanitizer.SanitizeString(longInput, 500);

        // Assert
        Assert.Equal(500, result.Length);
    }

    [Fact]
    public void SanitizeString_WithNullInput_ReturnsEmpty()
    {
        // Act
        var result = _sanitizer.SanitizeString(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeString_WithWhitespace_Trims()
    {
        // Act
        var result = _sanitizer.SanitizeString("  Hello  ");

        // Assert
        Assert.Equal("Hello", result);
    }

    // ============= ADDRESS SANITIZATION TESTS =============

    [Fact]
    public void SanitizeAddress_WithValidAddress_ReturnsAddress()
    {
        // Act
        var result = _sanitizer.SanitizeAddress("WOLF8A2C4F");

        // Assert
        Assert.Equal("WOLF8A2C4F", result);
    }

    [Fact]
    public void SanitizeAddress_WithEmpty_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeAddress(""));
    }

    [Fact]
    public void SanitizeAddress_WithSqlInjection_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeAddress("WOLF'; DROP TABLE;--"));
    }

    [Fact]
    public void SanitizeAddress_WithInvalidCharacters_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeAddress("WOLF<script>"));
    }

    // ============= EMAIL SANITIZATION TESTS =============

    [Fact]
    public void SanitizeEmail_WithValidEmail_ReturnsEmail()
    {
        // Act
        var result = _sanitizer.SanitizeEmail("user@example.com");

        // Assert
        Assert.Equal("user@example.com", result);
    }

    [Fact]
    public void SanitizeEmail_WithUppercase_ReturnsLowercase()
    {
        // Act
        var result = _sanitizer.SanitizeEmail("USER@EXAMPLE.COM");

        // Assert
        Assert.Equal("user@example.com", result);
    }

    [Fact]
    public void SanitizeEmail_WithInvalidFormat_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeEmail("not-an-email"));
    }

    [Fact]
    public void SanitizeEmail_WithTooLongEmail_ThrowsException()
    {
        // Arrange
        var longEmail = new string('a', 300) + "@example.com";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeEmail(longEmail));
    }

    // ============= DECIMAL SANITIZATION TESTS =============

    [Fact]
    public void SanitizeDecimal_WithValidValue_ReturnsValue()
    {
        // Act
        var result = _sanitizer.SanitizeDecimal(100.50m, 0, 1000);

        // Assert
        Assert.Equal(100.50m, result);
    }

    [Fact]
    public void SanitizeDecimal_WithValueBelowMin_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeDecimal(-10m, 0, 100));
    }

    [Fact]
    public void SanitizeDecimal_WithValueAboveMax_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeDecimal(5000m, 0, 1000));
    }

    // ============= INTEGER SANITIZATION TESTS =============

    [Fact]
    public void SanitizeInteger_WithValidValue_ReturnsValue()
    {
        // Act
        var result = _sanitizer.SanitizeInteger(50, 0, 100);

        // Assert
        Assert.Equal(50, result);
    }

    [Fact]
    public void SanitizeInteger_WithValueBelowMin_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeInteger(-10, 0, 100));
    }

    [Fact]
    public void SanitizeInteger_WithValueAboveMax_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sanitizer.SanitizeInteger(500, 0, 100));
    }
}
