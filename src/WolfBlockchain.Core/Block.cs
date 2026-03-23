using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WolfBlockchain.Core;

public class Block
{
    public int Index { get; set; }
    public DateTime Timestamp { get; set; }
    public string PreviousHash { get; set; }
    public string Hash { get; set; }
    public List<Transaction> Transactions { get; set; }
    public int Nonce { get; set; }
    public string Validator { get; set; }

    public Block(int index, DateTime timestamp, List<Transaction> transactions, string previousHash = "")
    {
        Index = index;
        Timestamp = timestamp;
        Transactions = transactions ?? new List<Transaction>();
        PreviousHash = previousHash;
        Nonce = 0;
        Validator = string.Empty;
        Hash = CalculateHash();
    }

    public string CalculateHash()
    {
        var blockData = $"{Index}{Timestamp:O}{PreviousHash}{JsonSerializer.Serialize(Transactions)}{Nonce}{Validator}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(blockData));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public void MineBlock(int difficulty)
    {
        var target = new string('0', difficulty);
        while (!Hash.StartsWith(target))
        {
            Nonce++;
            Hash = CalculateHash();
        }
        Console.WriteLine($"Block mined: {Hash}");
    }
}