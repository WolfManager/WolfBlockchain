namespace WolfBlockchain.Core;

/// <summary>
/// Executor pentru smart contracts
/// </summary>
public class ContractExecutor
{
    private Dictionary<string, SmartContract> _contracts;
    private List<ContractCall> _callHistory;
    private List<ContractReceipt> _receipts;

    public ContractExecutor()
    {
        _contracts = new Dictionary<string, SmartContract>();
        _callHistory = new List<ContractCall>();
        _receipts = new List<ContractReceipt>();
    }

    /// <summary>Deploy smart contract</summary>
    public SmartContract? DeployContract(string name, string description, SmartContractType type, 
        string creatorAddress, string bytecode, string abi)
    {
        var contract = new SmartContract(name, description, type, creatorAddress, bytecode, abi);

        if (!contract.IsValid())
        {
            Console.WriteLine("❌ Invalid contract");
            return null;
        }

        _contracts[contract.ContractId] = contract;
        Console.WriteLine($"✅ Contract deployed: {name} at {contract.Address}");
        return contract;
    }

    /// <summary>Obtine contract dupa ID</summary>
    public SmartContract? GetContract(string contractId)
    {
        return _contracts.ContainsKey(contractId) ? _contracts[contractId] : null;
    }

    /// <summary>Obtine contract dupa adresa</summary>
    public SmartContract? GetContractByAddress(string address)
    {
        return _contracts.Values.FirstOrDefault(c => c.Address == address);
    }

    /// <summary>Executa call pe contract</summary>
    public ContractReceipt? ExecuteCall(string contractId, string callerAddress, string functionName, 
        Dictionary<string, object>? parameters = null, decimal value = 0, long gasLimit = 21000)
    {
        var contract = GetContract(contractId);
        if (contract == null)
        {
            Console.WriteLine("❌ Contract not found");
            return null;
        }

        var call = new ContractCall(contractId, callerAddress, functionName)
        {
            Parameters = parameters ?? new Dictionary<string, object>(),
            Value = value,
            GasLimit = gasLimit
        };

        // Simulam executia funcției
        try
        {
            var result = ExecuteFunction(contract, functionName, call.Parameters);
            
            // Calculate gas used (simplified)
            var gasUsed = 1000 + (call.Parameters.Count * 100);
            
            call.Complete(result, gasUsed);
            _callHistory.Add(call);

            // Transfer value daca e cazul
            if (value > 0)
            {
                contract.Balance += value;
            }

            // Create receipt
            var receipt = new ContractReceipt(contract.Address)
            {
                GasUsed = gasUsed,
                IsSuccess = true,
                Output = result?.ToString() ?? "OK",
                ContractCalls = new List<string> { call.CallId }
            };

            _receipts.Add(receipt);

            Console.WriteLine($"✅ Function executed: {functionName}, Gas used: {gasUsed}");
            return receipt;
        }
        catch (Exception ex)
        {
            call.Fail(ex.Message);
            _callHistory.Add(call);

            var receipt = new ContractReceipt(contract.Address)
            {
                IsSuccess = false,
                Output = $"Error: {ex.Message}",
                GasUsed = call.GasUsed
            };

            _receipts.Add(receipt);

            Console.WriteLine($"❌ Execution failed: {ex.Message}");
            return receipt;
        }
    }

    /// <summary>Executa functie pe contract (simplu)</summary>
    private object? ExecuteFunction(SmartContract contract, string functionName, Dictionary<string, object> parameters)
    {
        // Switch pe tipul contractului și functia
        return functionName switch
        {
            // Token Transfer Functions
            "transfer" => HandleTransfer(contract, parameters),
            "approve" => HandleApprove(contract, parameters),
            "balanceOf" => HandleBalanceOf(contract, parameters),

            // AI Training Functions
            "startTraining" => HandleStartTraining(contract, parameters),
            "completeTraining" => HandleCompleteTraining(contract, parameters),

            // Staking Functions
            "stake" => HandleStake(contract, parameters),
            "unstake" => HandleUnstake(contract, parameters),

            // Governance Functions
            "vote" => HandleVote(contract, parameters),
            "propose" => HandlePropose(contract, parameters),

            // Generic state getter
            "getState" => HandleGetState(contract, parameters),

            _ => throw new InvalidOperationException($"Unknown function: {functionName}")
        };
    }

    #region Function Handlers

    private object? HandleTransfer(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("to") || !parameters.ContainsKey("amount"))
            throw new ArgumentException("Missing parameters: to, amount");

        var to = parameters["to"].ToString();
        var amount = decimal.Parse(parameters["amount"].ToString() ?? "0");

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        contract.UpdateState($"transfer_{to}", amount);
        contract.EmitEvent("Transfer", new Dictionary<string, object>
        {
            { "to", to },
            { "amount", amount },
            { "timestamp", DateTime.UtcNow }
        });

        return true;
    }

    private object? HandleApprove(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("spender") || !parameters.ContainsKey("amount"))
            throw new ArgumentException("Missing parameters: spender, amount");

        var spender = parameters["spender"].ToString();
        var amount = decimal.Parse(parameters["amount"].ToString() ?? "0");

        contract.UpdateState($"allowance_{spender}", amount);
        contract.EmitEvent("Approval", new Dictionary<string, object>
        {
            { "spender", spender },
            { "amount", amount }
        });

        return true;
    }

    private object? HandleBalanceOf(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("account"))
            throw new ArgumentException("Missing parameter: account");

        var account = parameters["account"].ToString();
        var balance = contract.GetStateValue($"balance_{account}") ?? 0;
        return balance;
    }

    private object? HandleStartTraining(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("modelId") || !parameters.ContainsKey("epochs"))
            throw new ArgumentException("Missing parameters: modelId, epochs");

        var modelId = parameters["modelId"].ToString();
        var epochs = int.Parse(parameters["epochs"].ToString() ?? "0");

        var jobId = Guid.NewGuid().ToString();
        contract.UpdateState($"training_job_{jobId}", new { modelId, epochs, status = "running" });
        contract.EmitEvent("TrainingStarted", new Dictionary<string, object>
        {
            { "jobId", jobId },
            { "modelId", modelId },
            { "epochs", epochs }
        });

        return jobId;
    }

    private object? HandleCompleteTraining(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("jobId") || !parameters.ContainsKey("accuracy"))
            throw new ArgumentException("Missing parameters: jobId, accuracy");

        var jobId = parameters["jobId"].ToString();
        var accuracy = decimal.Parse(parameters["accuracy"].ToString() ?? "0");

        contract.UpdateState($"training_result_{jobId}", accuracy);
        contract.EmitEvent("TrainingCompleted", new Dictionary<string, object>
        {
            { "jobId", jobId },
            { "accuracy", accuracy }
        });

        return true;
    }

    private object? HandleStake(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("amount"))
            throw new ArgumentException("Missing parameter: amount");

        var amount = decimal.Parse(parameters["amount"].ToString() ?? "0");

        if (amount <= 0)
            throw new ArgumentException("Stake amount must be greater than 0");

        var totalStaked = (decimal)(contract.GetStateValue("total_staked") ?? 0);
        contract.UpdateState("total_staked", totalStaked + amount);
        contract.EmitEvent("Staked", new Dictionary<string, object>
        {
            { "amount", amount },
            { "totalStaked", totalStaked + amount }
        });

        return true;
    }

    private object? HandleUnstake(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("amount"))
            throw new ArgumentException("Missing parameter: amount");

        var amount = decimal.Parse(parameters["amount"].ToString() ?? "0");
        var totalStaked = (decimal)(contract.GetStateValue("total_staked") ?? 0);

        if (amount > totalStaked)
            throw new ArgumentException("Cannot unstake more than total staked");

        contract.UpdateState("total_staked", totalStaked - amount);
        contract.EmitEvent("Unstaked", new Dictionary<string, object>
        {
            { "amount", amount },
            { "totalStaked", totalStaked - amount }
        });

        return true;
    }

    private object? HandleVote(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("proposalId") || !parameters.ContainsKey("choice"))
            throw new ArgumentException("Missing parameters: proposalId, choice");

        var proposalId = parameters["proposalId"].ToString();
        var choice = parameters["choice"].ToString();

        contract.EmitEvent("Voted", new Dictionary<string, object>
        {
            { "proposalId", proposalId },
            { "choice", choice }
        });

        return true;
    }

    private object? HandlePropose(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("description"))
            throw new ArgumentException("Missing parameter: description");

        var description = parameters["description"].ToString();
        var proposalId = Guid.NewGuid().ToString();

        contract.UpdateState($"proposal_{proposalId}", new { description, created = DateTime.UtcNow });
        contract.EmitEvent("ProposalCreated", new Dictionary<string, object>
        {
            { "proposalId", proposalId },
            { "description", description }
        });

        return proposalId;
    }

    private object? HandleGetState(SmartContract contract, Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("key"))
            throw new ArgumentException("Missing parameter: key");

        var key = parameters["key"].ToString();
        return contract.GetStateValue(key);
    }

    #endregion

    /// <summary>Obtine call history</summary>
    public List<ContractCall> GetCallHistory(string contractId)
    {
        return _callHistory.Where(c => c.ContractId == contractId).ToList();
    }

    /// <summary>Obtine receipts</summary>
    public List<ContractReceipt> GetReceipts(string contractId)
    {
        var contract = GetContract(contractId);
        return contract != null ? _receipts.Where(r => r.ContractAddress == contract.Address).ToList() : new List<ContractReceipt>();
    }

    /// <summary>Lista toti contractele</summary>
    public List<SmartContract> GetAllContracts()
    {
        return _contracts.Values.ToList();
    }

    /// <summary>Obtine statistici</summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            { "TotalContracts", _contracts.Count },
            { "TotalCalls", _callHistory.Count },
            { "SuccessfulCalls", _callHistory.Count(c => c.Status == ContractStatus.Completed) },
            { "FailedCalls", _callHistory.Count(c => c.Status == ContractStatus.Failed) },
            { "TotalReceipts", _receipts.Count },
            { "SuccessfulReceipts", _receipts.Count(r => r.IsSuccess) }
        };
    }
}
