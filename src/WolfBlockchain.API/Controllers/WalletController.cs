using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Storage;
using WalletClass = WolfBlockchain.Wallet.Wallet;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private static List<WalletClass> _wallets = new List<WalletClass>();
    private static BlockchainStorage _storage = new BlockchainStorage();

    [HttpPost("create")]
    public IActionResult CreateWallet()
    {
        var wallet = new WalletClass();
        _wallets.Add(wallet);
        _storage.SaveWallets(_wallets);
        return Ok(new
        {
            Message = "Wallet created successfully",
            Address = wallet.Address,
            PublicKey = wallet.PublicKey
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