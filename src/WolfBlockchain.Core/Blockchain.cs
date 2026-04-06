namespace WolfBlockchain.Core;

public class Blockchain
{
    public List<Block> Chain { get; set; }
    public int Difficulty { get; set; }
    public List<Transaction> PendingTransactions { get; set; }
    public decimal MiningReward { get; set; }

    public Blockchain()
    {
        Chain = new List<Block>();
        Difficulty = 2;
        PendingTransactions = new List<Transaction>();
        MiningReward = 50m;
        Chain.Add(CreateGenesisBlock());
    }

    private Block CreateGenesisBlock()
    {
        var genesisTransactions = new List<Transaction>
        {
            new Transaction("System", "Genesis", 1000000m)
        };
        var genesisBlock = new Block(0, DateTime.UtcNow, genesisTransactions, "0");
        return genesisBlock;
    }

    public Block GetLatestBlock()
    {
        return Chain[^1];
    }

    public void MinePendingTransactions(string minerAddress)
    {
        var rewardTx = new Transaction("System", minerAddress, MiningReward);
        PendingTransactions.Add(rewardTx);

        var block = new Block(Chain.Count, DateTime.UtcNow, PendingTransactions, GetLatestBlock().Hash);
        block.MineBlock(Difficulty);

        Console.WriteLine($"Block successfully mined! Reward: {MiningReward} WOLF");
        Chain.Add(block);

        PendingTransactions = new List<Transaction>();
    }

    public void AddTransaction(Transaction transaction)
    {
        if (!transaction.IsValid())
        {
            throw new Exception("Invalid transaction");
        }

        PendingTransactions.Add(transaction);
    }

    public decimal GetBalance(string address)
    {
        decimal balance = 0;

        foreach (var block in Chain)
        {
            foreach (var trans in block.Transactions)
            {
                if (trans.From == address)
                {
                    balance -= trans.Amount + trans.Fee;
                }

                if (trans.To == address)
                {
                    balance += trans.Amount;
                }
            }
        }

        return balance;
    }

    public bool IsChainValid()
    {
        for (int i = 1; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var previousBlock = Chain[i - 1];

            if (currentBlock.Hash != currentBlock.CalculateHash())
            {
                return false;
            }

            if (currentBlock.PreviousHash != previousBlock.Hash)
            {
                return false;
            }
        }
        return true;
    }

    public List<BlockHistoryEntry> GetHistory(int skip = 0, int take = 50)
    {
        return Chain
            .Skip(skip)
            .Take(take)
            .Select(b => b.ToHistoryEntry())
            .ToList();
    }
}

public record BlockHistoryEntry(
    int Index,
    string Hash,
    string PreviousHash,
    DateTime Timestamp,
    int TransactionCount,
    int Nonce,
    string Validator);