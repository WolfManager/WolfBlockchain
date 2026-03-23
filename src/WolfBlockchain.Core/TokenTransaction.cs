using System.Security.Cryptography;
using System.Text;

namespace WolfBlockchain.Core;

/// <summary>
/// Tranzactie de transfer token pe Wolf Blockchain
/// </summary>
public class TokenTransaction
{
    /// <summary>ID unic al tranzactiei de token</summary>
    public string TransactionId { get; set; }
    
    /// <summary>ID token-ului transferat</summary>
    public string TokenId { get; set; }
    
    /// <summary>Tipul de token</summary>
    public TokenType TokenType { get; set; }
    
    /// <summary>Adresa trimite</summary>
    public string FromAddress { get; set; }
    
    /// <summary>Adresa receiver</summary>
    public string ToAddress { get; set; }
    
    /// <summary>Cantitate transferata</summary>
    public decimal Amount { get; set; }
    
    /// <summary>Taxa de tranzactie (in Wolf Coin)</summary>
    public decimal Fee { get; set; }
    
    /// <summary>Data tranzactiei</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Starea tranzactiei</summary>
    public TransactionStatus Status { get; set; }
    
    /// <summary>Hash tranzactiei</summary>
    public string Hash { get; set; }
    
    /// <summary>Descriere/metadata</summary>
    public string? Description { get; set; }

    public TokenTransaction(string tokenId, TokenType tokenType, string from, string to, decimal amount, decimal fee = 0)
    {
        TokenId = tokenId;
        TokenType = tokenType;
        FromAddress = from;
        ToAddress = to;
        Amount = amount;
        Fee = fee;
        Timestamp = DateTime.UtcNow;
        Status = TransactionStatus.Pending;
        Hash = CalculateHash();
    }

    /// <summary>Calculeaza hash-ul tranzactiei</summary>
    public string CalculateHash()
    {
        var data = $"{TokenId}{TokenType}{FromAddress}{ToAddress}{Amount}{Fee}{Timestamp:O}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>Valideaza tranzactia de token</summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(TokenId) || string.IsNullOrEmpty(FromAddress) || string.IsNullOrEmpty(ToAddress))
            return false;

        if (Amount <= 0)
            return false;

        if (Fee < 0)
            return false;

        if (FromAddress == ToAddress)
            return false;

        return true;
    }
}

/// <summary>Starea unei tranzactii de token</summary>
public enum TransactionStatus
{
    /// <summary>In asteptare</summary>
    Pending = 0,
    
    /// <summary>Confirmata</summary>
    Confirmed = 1,
    
    /// <summary>Rejectata</summary>
    Rejected = 2,
    
    /// <summary>Esuata</summary>
    Failed = 3
}
