using System.Text.Json;
using WolfBlockchain.Core;

namespace WolfBlockchain.Storage;

public class BlockchainStorage
{
    private readonly string _dataPath;
    private readonly string _blockchainFile;
    private readonly string _walletsFile;

    public BlockchainStorage(string dataPath = "data")
    {
        _dataPath = dataPath;
        _blockchainFile = Path.Combine(_dataPath, "blockchain.json");
        _walletsFile = Path.Combine(_dataPath, "wallets.json");
        
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
    }

    public void SaveBlockchain(Blockchain blockchain)
    {
        var json = JsonSerializer.Serialize(blockchain, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_blockchainFile, json);
        Console.WriteLine($"Blockchain saved to {_blockchainFile}");
    }

    public Blockchain? LoadBlockchain()
    {
        if (!File.Exists(_blockchainFile))
        {
            Console.WriteLine("No blockchain file found. Creating new blockchain.");
            return null;
        }

        try
        {
            var json = File.ReadAllText(_blockchainFile);
            var blockchain = JsonSerializer.Deserialize<Blockchain>(json);
            Console.WriteLine($"Blockchain loaded from {_blockchainFile}");
            return blockchain;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading blockchain: {ex.Message}");
            return null;
        }
    }

    public void SaveWallets(List<WolfBlockchain.Wallet.Wallet> wallets)
    {
        var json = JsonSerializer.Serialize(wallets, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_walletsFile, json);
        Console.WriteLine($"Wallets saved to {_walletsFile}");
    }

    public List<WolfBlockchain.Wallet.Wallet>? LoadWallets()
    {
        if (!File.Exists(_walletsFile))
        {
            Console.WriteLine("No wallets file found.");
            return new List<WolfBlockchain.Wallet.Wallet>();
        }

        try
        {
            var json = File.ReadAllText(_walletsFile);
            var wallets = JsonSerializer.Deserialize<List<WolfBlockchain.Wallet.Wallet>>(json);
            Console.WriteLine($"Wallets loaded from {_walletsFile}");
            return wallets ?? new List<WolfBlockchain.Wallet.Wallet>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading wallets: {ex.Message}");
            return new List<WolfBlockchain.Wallet.Wallet>();
        }
    }
}