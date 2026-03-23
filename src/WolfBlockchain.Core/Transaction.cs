using System.Security.Cryptography;
using System.Text;

namespace WolfBlockchain.Core;

public class Transaction
{
    public string From { get; set; }
    public string To { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    public string Signature { get; set; }
    public string TransactionId { get; set; }

    public Transaction(string from, string to, decimal amount, decimal fee = 0)
    {
        From = from;
        To = to;
        Amount = amount;
        Fee = fee;
        Timestamp = DateTime.UtcNow;
        TransactionId = CalculateHash();
        Signature = string.Empty;
    }

    public string CalculateHash()
    {
        var rawData = $"{From}{To}{Amount}{Fee}{Timestamp:O}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLower();
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(From) || string.IsNullOrEmpty(To))
            return false;

        if (Amount <= 0)
            return false;

        return true;
    }
}