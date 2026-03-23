using WolfBlockchain.Core;
using WolfBlockchain.Wallet;

Console.WriteLine("========================================");
Console.WriteLine("    WOLF BLOCKCHAIN - PHASE 1");
Console.WriteLine("    AI-Powered Blockchain Network");
Console.WriteLine("========================================");
Console.WriteLine();

var wolfChain = new Blockchain();
Console.WriteLine($"Genesis Block Created: {wolfChain.Chain[0].Hash}");
Console.WriteLine();

var wallet1 = new Wallet();
var wallet2 = new Wallet();

Console.WriteLine($"Wallet 1 Address: {wallet1.Address}");
Console.WriteLine($"Wallet 2 Address: {wallet2.Address}");
Console.WriteLine();

Console.WriteLine("Mining first block...");
wolfChain.MinePendingTransactions(wallet1.Address);
Console.WriteLine($"Wallet 1 Balance: {wolfChain.GetBalance(wallet1.Address)} WOLF");
Console.WriteLine();

Console.WriteLine("Creating transaction: Wallet 1 -> Wallet 2 (10 WOLF)");
var tx1 = new Transaction(wallet1.Address, wallet2.Address, 10m, 0.1m);
wolfChain.AddTransaction(tx1);

Console.WriteLine("Mining second block...");
wolfChain.MinePendingTransactions(wallet1.Address);
Console.WriteLine($"Wallet 1 Balance: {wolfChain.GetBalance(wallet1.Address)} WOLF");
Console.WriteLine($"Wallet 2 Balance: {wolfChain.GetBalance(wallet2.Address)} WOLF");
Console.WriteLine();

Console.WriteLine($"Blockchain valid: {wolfChain.IsChainValid()}");
Console.WriteLine($"Total blocks in chain: {wolfChain.Chain.Count}");
Console.WriteLine();

Console.WriteLine("========================================");
Console.WriteLine("WOLF Blockchain is running!");
Console.WriteLine("========================================");