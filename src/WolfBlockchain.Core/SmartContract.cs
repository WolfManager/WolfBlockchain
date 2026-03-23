namespace WolfBlockchain.Core;

/// <summary>
/// Tipurile de smart contracts
/// </summary>
public enum SmartContractType
{
    /// <summary>Contract pentru transfer de tokeni</summary>
    TokenTransfer = 0,
    
    /// <summary>Contract pentru AI training</summary>
    AITraining = 1,
    
    /// <summary>Contract pentru staking</summary>
    Staking = 2,
    
    /// <summary>Contract pentru governance</summary>
    Governance = 3,
    
    /// <summary>Contract custom</summary>
    Custom = 4
}

/// <summary>
/// Starea contractului
/// </summary>
public enum ContractStatus
{
    /// <summary>Deployed pe blockchain</summary>
    Deployed = 0,
    
    /// <summary>In executie</summary>
    Executing = 1,
    
    /// <summary>Completat</summary>
    Completed = 2,
    
    /// <summary>Eșuat</summary>
    Failed = 3,
    
    /// <summary>Anulat</summary>
    Cancelled = 4
}

/// <summary>
/// Event din smart contract
/// </summary>
public class ContractEvent
{
    /// <summary>ID eveniment</summary>
    public string EventId { get; set; }
    
    /// <summary>Nume eveniment</summary>
    public string Name { get; set; }
    
    /// <summary>Data eveniment</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Date eveniment JSON</summary>
    public Dictionary<string, object> Data { get; set; }

    public ContractEvent(string name)
    {
        EventId = Guid.NewGuid().ToString();
        Name = name;
        Timestamp = DateTime.UtcNow;
        Data = new Dictionary<string, object>();
    }
}

/// <summary>
/// Smart Contract pentru Wolf Blockchain
/// </summary>
public class SmartContract
{
    /// <summary>ID unic al contractului</summary>
    public string ContractId { get; set; }
    
    /// <summary>Adresa contractului (similar cu Ethereum)</summary>
    public string Address { get; set; }
    
    /// <summary>Tipul contractului</summary>
    public SmartContractType Type { get; set; }
    
    /// <summary>Adresa creator-ului</summary>
    public string CreatorAddress { get; set; }
    
    /// <summary>Nume contract</summary>
    public string Name { get; set; }
    
    /// <summary>Descriere contract</summary>
    public string Description { get; set; }
    
    /// <summary>Versiune contract</summary>
    public string Version { get; set; }
    
    /// <summary>Codul contractului (Base64 encoded bytecode)</summary>
    public string Bytecode { get; set; }
    
    /// <summary>ABI - Interface definiție</summary>
    public string ABI { get; set; }
    
    /// <summary>Status curent</summary>
    public ContractStatus Status { get; set; }
    
    /// <summary>Data deploy</summary>
    public DateTime DeployedAt { get; set; }
    
    /// <summary>Stare contract (state variables)</summary>
    public Dictionary<string, object> State { get; set; }
    
    /// <summary>Balanța contract (in WOLF)</summary>
    public decimal Balance { get; set; }
    
    /// <summary>Evenimete emise de contract</summary>
    public List<ContractEvent> Events { get; set; }
    
    /// <summary>Hash contract</summary>
    public string ContractHash { get; set; }

    public SmartContract(string name, string description, SmartContractType type, string creatorAddress, string bytecode, string abi)
    {
        ContractId = Guid.NewGuid().ToString();
        Address = GenerateContractAddress();
        Name = name;
        Description = description;
        Type = type;
        CreatorAddress = creatorAddress;
        Version = "1.0.0";
        Bytecode = bytecode;
        ABI = abi;
        Status = ContractStatus.Deployed;
        DeployedAt = DateTime.UtcNow;
        State = new Dictionary<string, object>();
        Balance = 0;
        Events = new List<ContractEvent>();
        ContractHash = SecurityUtils.HashSHA256($"{ContractId}{CreatorAddress}{DeployedAt:O}");
    }

    /// <summary>Genereaza o adresa unica pentru contract (similar Ethereum)</summary>
    private string GenerateContractAddress()
    {
        var nonce = Guid.NewGuid().ToString();
        var hash = SecurityUtils.HashSHA256($"CONTRACT_{nonce}_{DateTime.UtcNow:O}");
        return "0x" + hash.Substring(0, 40).ToUpper();
    }

    /// <summary>Valideaza contractul</summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && 
               !string.IsNullOrEmpty(Bytecode) && 
               !string.IsNullOrEmpty(ABI) &&
               !string.IsNullOrEmpty(CreatorAddress);
    }

    /// <summary>Emite un event</summary>
    public void EmitEvent(string eventName, Dictionary<string, object> data)
    {
        var evt = new ContractEvent(eventName) { Data = data };
        Events.Add(evt);
    }

    /// <summary>Actualizeaza state</summary>
    public void UpdateState(string key, object value)
    {
        State[key] = value;
    }

    /// <summary>Obtine valoare din state</summary>
    public object? GetStateValue(string key)
    {
        return State.ContainsKey(key) ? State[key] : null;
    }
}

/// <summary>
/// Call la functie de contract
/// </summary>
public class ContractCall
{
    /// <summary>ID call</summary>
    public string CallId { get; set; }
    
    /// <summary>ID Contract</summary>
    public string ContractId { get; set; }
    
    /// <summary>Adresa caller-ului</summary>
    public string CallerAddress { get; set; }
    
    /// <summary>Nume functie</summary>
    public string FunctionName { get; set; }
    
    /// <summary>Parametri functie JSON</summary>
    public Dictionary<string, object> Parameters { get; set; }
    
    /// <summary>Valoare transferata cu callul (in WOLF)</summary>
    public decimal Value { get; set; }
    
    /// <summary>Gas limit</summary>
    public long GasLimit { get; set; }
    
    /// <summary>Gas actual folosit</summary>
    public long GasUsed { get; set; }
    
    /// <summary>Rezultat executie</summary>
    public object? ReturnValue { get; set; }
    
    /// <summary>Timestamp callul</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Mesaj eroare (daca e cazul)</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Status call</summary>
    public ContractStatus Status { get; set; }

    public ContractCall(string contractId, string callerAddress, string functionName)
    {
        CallId = Guid.NewGuid().ToString();
        ContractId = contractId;
        CallerAddress = callerAddress;
        FunctionName = functionName;
        Parameters = new Dictionary<string, object>();
        Value = 0;
        GasLimit = 21000;
        GasUsed = 0;
        ReturnValue = null;
        Timestamp = DateTime.UtcNow;
        ErrorMessage = null;
        Status = ContractStatus.Executing;
    }

    /// <summary>Marchez callul ca completat</summary>
    public void Complete(object returnValue, long gasUsed)
    {
        ReturnValue = returnValue;
        GasUsed = gasUsed;
        Status = ContractStatus.Completed;
    }

    /// <summary>Marchez callul ca eșuat</summary>
    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = ContractStatus.Failed;
    }
}

/// <summary>
/// Receipt de executie contract
/// </summary>
public class ContractReceipt
{
    /// <summary>ID transaction</summary>
    public string TransactionHash { get; set; }
    
    /// <summary>ID Contract</summary>
    public string ContractAddress { get; set; }
    
    /// <summary>Gas folosit</summary>
    public long GasUsed { get; set; }
    
    /// <summary>Status executie</summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>Mesaj output</summary>
    public string Output { get; set; }
    
    /// <summary>Block number</summary>
    public int BlockNumber { get; set; }
    
    /// <summary>Timestamp</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Calturi executate</summary>
    public List<string> ContractCalls { get; set; }

    public ContractReceipt(string contractAddress)
    {
        TransactionHash = SecurityUtils.HashSHA256($"TX_{Guid.NewGuid()}_{DateTime.UtcNow:O}");
        ContractAddress = contractAddress;
        GasUsed = 0;
        IsSuccess = true;
        Output = "";
        BlockNumber = 0;
        Timestamp = DateTime.UtcNow;
        ContractCalls = new List<string>();
    }
}
