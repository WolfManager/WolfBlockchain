namespace WolfBlockchain.Core;

/// <summary>
/// Manager pentru tokeni pe Wolf Blockchain
/// Gestioneaza crearea, transferul si validarea tokenilor
/// </summary>
public class TokenManager
{
    /// <summary>Dictionar de tokeni (TokenId -> Token)</summary>
    private Dictionary<string, Token> _tokens;
    
    /// <summary>Dictionar de balanțe token per adresa (Address -> (TokenId -> Amount))</summary>
    private Dictionary<string, Dictionary<string, decimal>> _tokenBalances;
    
    /// <summary>Istoric de tranzactii de token</summary>
    private List<TokenTransaction> _tokenTransactionHistory;
    
    /// <summary>Adresa proprietarului blockchain (cu control total)</summary>
    public string OwnerAddress { get; set; }

    public TokenManager(string ownerAddress)
    {
        OwnerAddress = ownerAddress;
        _tokens = new Dictionary<string, Token>();
        _tokenBalances = new Dictionary<string, Dictionary<string, decimal>>();
        _tokenTransactionHistory = new List<TokenTransaction>();
    }

    /// <summary>Creeaza un nou token</summary>
    public Token? CreateToken(string name, string symbol, TokenType type, string creatorAddress, decimal totalSupply, int decimals = 18)
    {
        if (string.IsNullOrEmpty(creatorAddress))
            return null;

        var token = new Token(name, symbol, type, creatorAddress, totalSupply, decimals);

        if (!token.IsValid())
            return null;

        _tokens[token.TokenId] = token;

        // Inițializam balanța creatorului cu supply-ul total
        if (!_tokenBalances.ContainsKey(creatorAddress))
            _tokenBalances[creatorAddress] = new Dictionary<string, decimal>();

        _tokenBalances[creatorAddress][token.TokenId] = totalSupply;

        Console.WriteLine($"Token created: {name} ({symbol}) - ID: {token.TokenId}");
        return token;
    }

    /// <summary>Obtine un token dupa ID</summary>
    public Token? GetToken(string tokenId)
    {
        return _tokens.ContainsKey(tokenId) ? _tokens[tokenId] : null;
    }

    /// <summary>Lista toti tokenii</summary>
    public List<Token> GetAllTokens()
    {
        return _tokens.Values.ToList();
    }

    /// <summary>Transfer token intre adrese</summary>
    public TokenTransaction? TransferToken(string tokenId, string from, string to, decimal amount, decimal fee = 0)
    {
        var token = GetToken(tokenId);
        if (token == null || !token.IsActive)
        {
            Console.WriteLine($"Token {tokenId} not found or inactive");
            return null;
        }

        // Verifica balanța
        var fromBalance = GetTokenBalance(from, tokenId);
        if (fromBalance < amount + fee)
        {
            Console.WriteLine($"Insufficient balance. Have: {fromBalance}, Need: {amount + fee}");
            return null;
        }

        // Creeaza tranzactia
        var transaction = new TokenTransaction(tokenId, token.Type, from, to, amount, fee);

        if (!transaction.IsValid())
        {
            Console.WriteLine("Invalid token transaction");
            return null;
        }

        // Actualizeaza balanțele
        if (!_tokenBalances.ContainsKey(from))
            _tokenBalances[from] = new Dictionary<string, decimal>();
        if (!_tokenBalances.ContainsKey(to))
            _tokenBalances[to] = new Dictionary<string, decimal>();

        _tokenBalances[from][tokenId] = fromBalance - amount - fee;
        _tokenBalances[to][tokenId] = GetTokenBalance(to, tokenId) + amount;

        // Taxa merge la owner
        if (fee > 0)
        {
            if (!_tokenBalances.ContainsKey(OwnerAddress))
                _tokenBalances[OwnerAddress] = new Dictionary<string, decimal>();
            
            // Fee se ia din tokenul Wolf (id-ul trebuie sa existe)
            // Pentru simplitate, adaugam fee direct la owner
        }

        transaction.Status = TransactionStatus.Confirmed;
        _tokenTransactionHistory.Add(transaction);

        Console.WriteLine($"Transfer: {amount} {token.Symbol} from {from} to {to}");
        return transaction;
    }

    /// <summary>Obtine balanța de token pentru o adresa</summary>
    public decimal GetTokenBalance(string address, string tokenId)
    {
        if (!_tokenBalances.ContainsKey(address))
            return 0;

        return _tokenBalances[address].ContainsKey(tokenId) ? _tokenBalances[address][tokenId] : 0;
    }

    /// <summary>Obtine toate balanțele de tokeni pentru o adresa</summary>
    public Dictionary<string, decimal> GetAllTokenBalances(string address)
    {
        return _tokenBalances.ContainsKey(address) ? new Dictionary<string, decimal>(_tokenBalances[address]) : new Dictionary<string, decimal>();
    }

    /// <summary>Obtine istoricul tranzactiilor de token pentru o adresa</summary>
    public List<TokenTransaction> GetTokenTransactionHistory(string address)
    {
        return _tokenTransactionHistory
            .Where(t => t.FromAddress == address || t.ToAddress == address)
            .ToList();
    }

    /// <summary>Obtine toate tranzactiile de token</summary>
    public List<TokenTransaction> GetAllTokenTransactions()
    {
        return new List<TokenTransaction>(_tokenTransactionHistory);
    }

    /// <summary>Minting - creaza noi unitati de token</summary>
    public bool MintToken(string tokenId, decimal amount, string targetAddress)
    {
        // Doar owner-ul poate face mint
        if (targetAddress != OwnerAddress)
        {
            Console.WriteLine("Only owner can mint tokens");
            return false;
        }

        var token = GetToken(tokenId);
        if (token == null)
        {
            Console.WriteLine($"Token {tokenId} not found");
            return false;
        }

        if (!token.Mint(amount))
        {
            Console.WriteLine($"Cannot mint {amount} tokens. Supply limit exceeded");
            return false;
        }

        if (!_tokenBalances.ContainsKey(targetAddress))
            _tokenBalances[targetAddress] = new Dictionary<string, decimal>();

        _tokenBalances[targetAddress][tokenId] = GetTokenBalance(targetAddress, tokenId) + amount;

        Console.WriteLine($"Minted {amount} {token.Symbol}");
        return true;
    }

    /// <summary>Burning - distruge unitati de token</summary>
    public bool BurnToken(string tokenId, decimal amount, string fromAddress)
    {
        var token = GetToken(tokenId);
        if (token == null)
        {
            Console.WriteLine($"Token {tokenId} not found");
            return false;
        }

        var balance = GetTokenBalance(fromAddress, tokenId);
        if (balance < amount)
        {
            Console.WriteLine($"Insufficient balance for burn. Have: {balance}, Need: {amount}");
            return false;
        }

        if (!token.Burn(amount))
        {
            Console.WriteLine("Cannot burn tokens");
            return false;
        }

        _tokenBalances[fromAddress][tokenId] -= amount;

        Console.WriteLine($"Burned {amount} {token.Symbol}");
        return true;
    }
}
