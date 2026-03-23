using System.Security.Cryptography;
using System.Text;

namespace WolfBlockchain.Core;

/// <summary>
/// Reprezentare a unui token pe Wolf Blockchain
/// </summary>
public class Token
{
    /// <summary>ID unic al tokenului</summary>
    public string TokenId { get; set; }
    
    /// <summary>Tipul de token</summary>
    public TokenType Type { get; set; }
    
    /// <summary>Nume token</summary>
    public string Name { get; set; }
    
    /// <summary>Simbol token (ex: WOLF, MEM, AI)</summary>
    public string Symbol { get; set; }
    
    /// <summary>Adresa creatorului tokenului</summary>
    public string CreatorAddress { get; set; }
    
    /// <summary>Supply total al tokenului</summary>
    public decimal TotalSupply { get; set; }
    
    /// <summary>Supply curent disponibil</summary>
    public decimal CurrentSupply { get; set; }
    
    /// <summary>Decimale pentru token</summary>
    public int Decimals { get; set; }
    
    /// <summary>Data crearii tokenului</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>Daca tokenul este activ</summary>
    public bool IsActive { get; set; }
    
    /// <summary>Metadate suplimentare</summary>
    public Dictionary<string, string> Metadata { get; set; }

    public Token(string name, string symbol, TokenType type, string creatorAddress, decimal totalSupply, int decimals = 18)
    {
        Name = name;
        Symbol = symbol;
        Type = type;
        CreatorAddress = creatorAddress;
        TotalSupply = totalSupply;
        CurrentSupply = totalSupply;
        Decimals = decimals;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        Metadata = new Dictionary<string, string>();
        TokenId = GenerateTokenId();
    }

    /// <summary>Genereaza ID unic pentru token</summary>
    private string GenerateTokenId()
    {
        var data = $"{Name}{Symbol}{CreatorAddress}{CreatedAt:O}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return "TOKEN" + Convert.ToHexString(hash)[..32].ToUpper();
    }

    /// <summary>Valideaza tokenul</summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Symbol))
            return false;

        if (TotalSupply <= 0 || CurrentSupply < 0)
            return false;

        if (Decimals < 0 || Decimals > 18)
            return false;

        if (CurrentSupply > TotalSupply)
            return false;

        return true;
    }

    /// <summary>Scade supply-ul disponibil (la mint)</summary>
    public bool Mint(decimal amount)
    {
        if (CurrentSupply + amount > TotalSupply)
            return false;

        CurrentSupply += amount;
        return true;
    }

    /// <summary>Scade supply-ul (la burn)</summary>
    public bool Burn(decimal amount)
    {
        if (CurrentSupply - amount < 0)
            return false;

        CurrentSupply -= amount;
        return true;
    }
}
