using System.Security.Cryptography;
using System.Text;

namespace WolfBlockchain.Wallet;

public class Wallet
{
    public string Address { get; private set; }
    public string PublicKey { get; private set; }
    public string PrivateKey { get; private set; }
    public decimal Balance { get; set; }
    
    // Suport pentru multipli tokeni
    public Dictionary<string, decimal> TokenBalances { get; set; }

    public Wallet()
    {
        GenerateKeys();
        TokenBalances = new Dictionary<string, decimal>();
    }

    private void GenerateKeys()
    {
        using var rsa = RSA.Create(2048);
        PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        Address = GenerateAddress(PublicKey);
    }

    private string GenerateAddress(string publicKey)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(publicKey));
        return "WOLF" + Convert.ToHexString(hash)[..40].ToUpper();
    }

    public decimal GetTokenBalance(string tokenId)
    {
        return TokenBalances.ContainsKey(tokenId) ? TokenBalances[tokenId] : 0;
    }

    public void AddTokenBalance(string tokenId, decimal amount)
    {
        if (!TokenBalances.ContainsKey(tokenId))
            TokenBalances[tokenId] = 0;
        TokenBalances[tokenId] += amount;
    }

    public bool SubtractTokenBalance(string tokenId, decimal amount)
    {
        var current = GetTokenBalance(tokenId);
        if (current < amount)
            return false;
        TokenBalances[tokenId] -= amount;
        return true;
    }
}