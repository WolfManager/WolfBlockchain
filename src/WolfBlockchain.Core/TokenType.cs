namespace WolfBlockchain.Core;

/// <summary>
/// Tipuri de tokeni suportate pe Wolf Blockchain
/// </summary>
public enum TokenType
{
    /// <summary>Token nativ Wolf Blockchain</summary>
    Wolf = 0,
    
    /// <summary>MemeCoin - token memetic</summary>
    MemeCoin = 1,
    
    /// <summary>Token AI - pentru antrenare si operatiuni AI</summary>
    TokenAI = 2,
    
    /// <summary>Coin AI - alt tip de token AI</summary>
    CoinAI = 3,
    
    /// <summary>Token custom utilizator</summary>
    Custom = 4
}
