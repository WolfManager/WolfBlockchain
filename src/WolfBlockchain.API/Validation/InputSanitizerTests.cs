namespace WolfBlockchain.API.Validation;

/// <summary>
/// Unit tests pentru InputSanitizer - PRODUCTION GRADE
/// </summary>
public class InputSanitizerTests
{
    private readonly IInputSanitizer _sanitizer = new InputSanitizer();

    // ============= STRING SANITIZATION TESTS =============
    
    /// <summary>Test sanitization of valid strings</summary>
    public void TestSanitizeString_ValidInput()
    {
        // Arrange
        var input = "Hello World";
        var maxLength = 500;

        // Act
        var result = _sanitizer.SanitizeString(input, maxLength);

        // Assert
        assert(result == "Hello World", "Valid string should not be modified");
    }

    /// <summary>Test XSS prevention in strings</summary>
    public void TestSanitizeString_XssPrevention()
    {
        // Arrange
        var xssInput = "<script>alert('XSS')</script>";

        // Act
        var result = _sanitizer.SanitizeString(xssInput);

        // Assert
        assert(!result.Contains("<"), "HTML tags should be removed");
        assert(!result.Contains(">"), "HTML tags should be removed");
        assert(!result.Contains("script"), "Script tags should not remain");
    }

    /// <summary>Test string truncation</summary>
    public void TestSanitizeString_Truncation()
    {
        // Arrange
        var longInput = new string('a', 600);
        var maxLength = 500;

        // Act
        var result = _sanitizer.SanitizeString(longInput, maxLength);

        // Assert
        assert(result.Length == maxLength, $"String should be truncated to {maxLength}");
    }

    // ============= ADDRESS SANITIZATION TESTS =============

    /// <summary>Test valid blockchain address</summary>
    public void TestSanitizeAddress_ValidAddress()
    {
        // Arrange
        var validAddress = "WOLF8A2C4F";

        // Act
        var result = _sanitizer.SanitizeAddress(validAddress);

        // Assert
        assert(result == validAddress, "Valid address should be accepted");
    }

    /// <summary>Test SQL injection prevention in address</summary>
    public void TestSanitizeAddress_SqlInjectionPrevention()
    {
        // Arrange
        var sqlInjection = "WOLF'; DROP TABLE Users;--";

        // Act & Assert
        try
        {
            _sanitizer.SanitizeAddress(sqlInjection);
            assert(false, "SQL injection should be blocked");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    /// <summary>Test empty address rejection</summary>
    public void TestSanitizeAddress_EmptyAddress()
    {
        // Arrange & Act & Assert
        try
        {
            _sanitizer.SanitizeAddress("");
            assert(false, "Empty address should be rejected");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    // ============= EMAIL SANITIZATION TESTS =============

    /// <summary>Test valid email</summary>
    public void TestSanitizeEmail_ValidEmail()
    {
        // Arrange
        var validEmail = "user@example.com";

        // Act
        var result = _sanitizer.SanitizeEmail(validEmail);

        // Assert
        assert(result == validEmail, "Valid email should be accepted");
    }

    /// <summary>Test invalid email rejection</summary>
    public void TestSanitizeEmail_InvalidEmail()
    {
        // Arrange
        var invalidEmail = "not-an-email";

        // Act & Assert
        try
        {
            _sanitizer.SanitizeEmail(invalidEmail);
            assert(false, "Invalid email should be rejected");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    /// <summary>Test email normalization</summary>
    public void TestSanitizeEmail_Normalization()
    {
        // Arrange
        var email = "USER@EXAMPLE.COM";

        // Act
        var result = _sanitizer.SanitizeEmail(email);

        // Assert
        assert(result == "user@example.com", "Email should be converted to lowercase");
    }

    // ============= DECIMAL SANITIZATION TESTS =============

    /// <summary>Test valid decimal</summary>
    public void TestSanitizeDecimal_ValidValue()
    {
        // Arrange
        var value = 100.50m;

        // Act
        var result = _sanitizer.SanitizeDecimal(value, 0, 1000);

        // Assert
        assert(result == value, "Valid decimal should be accepted");
    }

    /// <summary>Test decimal out of range rejection</summary>
    public void TestSanitizeDecimal_OutOfRange()
    {
        // Arrange
        var value = 5000m;

        // Act & Assert
        try
        {
            _sanitizer.SanitizeDecimal(value, 0, 1000);
            assert(false, "Out of range decimal should be rejected");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    // ============= INTEGER SANITIZATION TESTS =============

    /// <summary>Test valid integer</summary>
    public void TestSanitizeInteger_ValidValue()
    {
        // Arrange
        var value = 50;

        // Act
        var result = _sanitizer.SanitizeInteger(value, 0, 100);

        // Assert
        assert(result == value, "Valid integer should be accepted");
    }

    /// <summary>Test negative integer rejection</summary>
    public void TestSanitizeInteger_Negative()
    {
        // Arrange & Act & Assert
        try
        {
            _sanitizer.SanitizeInteger(-10, 0, 100);
            assert(false, "Negative integer outside range should be rejected");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    // ============= HELPER METHOD =============

    private void assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception($"Test failed: {message}");
    }
}
