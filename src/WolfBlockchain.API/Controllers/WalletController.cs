using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Storage;
using WalletClass = WolfBlockchain.Wallet.Wallet;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private static List<WalletStorageEntry> _wallets = new List<WalletStorageEntry>();
    private static BlockchainStorage _storage = new BlockchainStorage();

    [HttpPost("create")]
    public IActionResult CreateWallet()
    {
        var wallet = new WalletClass();
        var entry = new WalletStorageEntry
        {
            Address = wallet.Address,
            PublicKey = wallet.PublicKey,
            PrivateKey = wallet.PrivateKey,
            Balance = wallet.Balance,
            TokenBalances = new Dictionary<string, decimal>(wallet.TokenBalances)
        };
        _wallets.Add(entry);
        _storage.SaveWallets(_wallets);
        return Ok(new
        {
            Message = "Wallet created successfully",
            Address = entry.Address,
            PublicKey = entry.PublicKey
        });
    }

    [HttpGet("list")]
    public IActionResult GetWallets()
    {
        return Ok(_wallets.Select(w => new
        {
            w.Address,
            w.Balance
        }));
    }

    [HttpGet("{address}")]
    public IActionResult GetWallet(string address)
    {
        var wallet = _wallets.FirstOrDefault(w => w.Address == address);
        if (wallet == null)
        {
            return NotFound("Wallet not found");
        }
        return Ok(new
        {
            wallet.Address,
            wallet.PublicKey,
            wallet.Balance
        });
    }

    [HttpPost("load")]
    public IActionResult LoadWallets()
    {
        var loaded = _storage.LoadWallets();
        if (loaded != null)
        {
            _wallets = loaded;
            return Ok(new { Message = "Wallets loaded successfully", Count = _wallets.Count });
        }
        return NotFound("No saved wallets found");
    }
}