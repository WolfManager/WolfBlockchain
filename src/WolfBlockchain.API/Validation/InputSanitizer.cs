using System.Text.RegularExpressions;

namespace WolfBlockchain.API.Validation;

/// <summary>
/// Input Sanitizer - Protecție contra XSS și SQL injection - PRODUCTION GRADE
/// </summary>
public interface IInputSanitizer
{
    string SanitizeString(string input, int maxLength = 500);
    string SanitizeAddress(string address);
    string SanitizeEmail(string email);
    decimal SanitizeDecimal(decimal value, decimal min = 0, decimal max = decimal.MaxValue);
    int SanitizeInteger(int value, int min = 0, int max = int.MaxValue);
}

/// <summary>
/// Implementare Input Sanitizer
/// </summary>
public class InputSanitizer : IInputSanitizer
{
    private static readonly Regex AddressPattern = new("^[A-Za-z0-9]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex SqlInjectionPattern = new(@"(')|(;)|(--)|(\/\*|\*\/)|(xp_)|(sp_)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sanitizeaza string - Protecție contra XSS
    /// </summary>
    public string SanitizeString(string input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Truncate la max length
        var sanitized = input.Length > maxLength 
            ? input.Substring(0, maxLength) 
            : input;

        // Remove dangerous HTML/JS characters
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[<>""'`]", "");

        // Trim whitespace
        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizeaza blockchain address
    /// </summary>
    public string SanitizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty");

        var sanitized = address.Trim();

        if (!AddressPattern.IsMatch(sanitized))
            throw new ArgumentException("Invalid address format");

        if (SqlInjectionPattern.IsMatch(sanitized))
            throw new ArgumentException("Address contains invalid characters");

        return sanitized;
    }

    /// <summary>
    /// Sanitizeaza email
    /// </summary>
    public string SanitizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty");

        var sanitized = email.Trim().ToLower();

        if (!EmailPattern.IsMatch(sanitized))
            throw new ArgumentException("Invalid email format");

        if (sanitized.Length > 254) // RFC 5321
            throw new ArgumentException("Email too long");

        return sanitized;
    }

    /// <summary>
    /// Sanitizeaza decimal value
    /// </summary>
    public decimal SanitizeDecimal(decimal value, decimal min = 0, decimal max = decimal.MaxValue)
    {
        if (value < min || value > max)
            throw new ArgumentException($"Value must be between {min} and {max}");

        return value;
    }

    /// <summary>
    /// Sanitizeaza integer value
    /// </summary>
    public int SanitizeInteger(int value, int min = 0, int max = int.MaxValue)
    {
        if (value < min || value > max)
            throw new ArgumentException($"Value must be between {min} and {max}");

        return value;
    }
}
