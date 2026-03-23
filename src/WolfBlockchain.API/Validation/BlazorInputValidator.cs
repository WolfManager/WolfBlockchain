namespace WolfBlockchain.API.Validation;

/// <summary>
/// Validation service for Blazor component inputs.
/// Provides centralized validation logic for forms.
/// </summary>
public static class BlazorInputValidator
{
    /// <summary>
    /// Validate token name.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateTokenName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Token name is required");

        if (name.Length < 3)
            return (false, "Token name must be at least 3 characters");

        if (name.Length > 100)
            return (false, "Token name cannot exceed 100 characters");

        if (!name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-'))
            return (false, "Token name can only contain letters, digits, spaces, and hyphens");

        return (true, null);
    }

    /// <summary>
    /// Validate token symbol (e.g., WOLF, BTC).
    /// </summary>
    public static (bool IsValid, string? Error) ValidateTokenSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return (false, "Token symbol is required");

        if (symbol.Length < 2)
            return (false, "Token symbol must be at least 2 characters");

        if (symbol.Length > 10)
            return (false, "Token symbol cannot exceed 10 characters");

        if (!symbol.All(char.IsLetterOrDigit))
            return (false, "Token symbol can only contain letters and digits");

        return (true, null);
    }

    /// <summary>
    /// Validate token supply amount.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateTokenSupply(long amount)
    {
        if (amount <= 0)
            return (false, "Supply must be greater than 0");

        if (amount > long.MaxValue / 2) // Reasonable upper limit
            return (false, "Supply exceeds maximum allowed");

        return (true, null);
    }

    /// <summary>
    /// Validate contract name.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateContractName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Contract name is required");

        if (name.Length < 3)
            return (false, "Contract name must be at least 3 characters");

        if (name.Length > 100)
            return (false, "Contract name cannot exceed 100 characters");

        return (true, null);
    }

    /// <summary>
    /// Validate contract code.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateContractCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Contract code is required");

        if (code.Length < 50)
            return (false, "Contract code seems too short");

        if (code.Length > 100_000) // 100KB max
            return (false, "Contract code exceeds maximum size (100KB)");

        return (true, null);
    }

    /// <summary>
    /// Validate training job name.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateJobName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Job name is required");

        if (name.Length < 3)
            return (false, "Job name must be at least 3 characters");

        if (name.Length > 200)
            return (false, "Job name cannot exceed 200 characters");

        return (true, null);
    }

    /// <summary>
    /// Validate dataset URL.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateDatasetUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "Dataset URL is required");

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return (false, "Dataset URL must start with http:// or https://");

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return (false, "Invalid dataset URL format");

        return (true, null);
    }

    /// <summary>
    /// Validate JSON parameters string.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateJsonParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (false, "Parameters required");

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return (true, null);
        }
        catch
        {
            return (false, "Invalid JSON format in parameters");
        }
    }
}
