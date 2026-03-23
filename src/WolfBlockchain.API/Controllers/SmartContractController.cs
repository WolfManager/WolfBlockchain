using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Core;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmartContractController : ControllerBase
{
    private static ContractExecutor _executor = new ContractExecutor();

    /// <summary>
    /// Deploy smart contract
    /// </summary>
    [HttpPost("deploy")]
    public IActionResult DeployContract([FromBody] DeployContractRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (!Enum.TryParse<SmartContractType>(request.Type, out var contractType))
            return BadRequest("Invalid contract type");

        var contract = _executor.DeployContract(request.Name, request.Description, contractType, 
            request.CreatorAddress, request.Bytecode, request.ABI);

        if (contract == null)
            return BadRequest("Failed to deploy contract");

        return Ok(new
        {
            success = true,
            contractId = contract.ContractId,
            address = contract.Address,
            name = contract.Name,
            type = contract.Type.ToString(),
            deployedAt = contract.DeployedAt
        });
    }

    /// <summary>
    /// Obtine informatii contract
    /// </summary>
    [HttpGet("{contractId}")]
    public IActionResult GetContract(string contractId)
    {
        var contract = _executor.GetContract(contractId);
        if (contract == null)
            return NotFound("Contract not found");

        return Ok(new
        {
            contractId = contract.ContractId,
            address = contract.Address,
            name = contract.Name,
            description = contract.Description,
            type = contract.Type.ToString(),
            creatorAddress = contract.CreatorAddress,
            status = contract.Status.ToString(),
            balance = contract.Balance,
            deployedAt = contract.DeployedAt,
            eventCount = contract.Events.Count,
            state = contract.State
        });
    }

    /// <summary>
    /// Obtine contract dupa adresa
    /// </summary>
    [HttpGet("address/{address}")]
    public IActionResult GetContractByAddress(string address)
    {
        var contract = _executor.GetContractByAddress(address);
        if (contract == null)
            return NotFound("Contract not found");

        return Ok(new
        {
            contractId = contract.ContractId,
            address = contract.Address,
            name = contract.Name,
            type = contract.Type.ToString(),
            balance = contract.Balance
        });
    }

    /// <summary>
    /// Executa functie pe contract
    /// </summary>
    [HttpPost("{contractId}/call")]
    public IActionResult ExecuteCall(string contractId, [FromBody] ExecuteCallRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var receipt = _executor.ExecuteCall(contractId, request.CallerAddress, request.FunctionName,
            request.Parameters, request.Value, request.GasLimit);

        if (receipt == null)
            return BadRequest("Failed to execute call");

        return Ok(new
        {
            success = receipt.IsSuccess,
            transactionHash = receipt.TransactionHash,
            contractAddress = receipt.ContractAddress,
            gasUsed = receipt.GasUsed,
            output = receipt.Output,
            timestamp = receipt.Timestamp
        });
    }

    /// <summary>
    /// Transfer tokeni prin contract
    /// </summary>
    [HttpPost("{contractId}/transfer")]
    public IActionResult Transfer(string contractId, [FromBody] TransferRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var parameters = new Dictionary<string, object>
        {
            { "to", request.To },
            { "amount", request.Amount }
        };

        var receipt = _executor.ExecuteCall(contractId, request.From, "transfer", parameters);

        if (receipt == null)
            return BadRequest("Transfer failed");

        return Ok(new
        {
            success = receipt.IsSuccess,
            transactionHash = receipt.TransactionHash,
            from = request.From,
            to = request.To,
            amount = request.Amount,
            gasUsed = receipt.GasUsed
        });
    }

    /// <summary>
    /// Approve spending pe contract
    /// </summary>
    [HttpPost("{contractId}/approve")]
    public IActionResult Approve(string contractId, [FromBody] ApproveRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var parameters = new Dictionary<string, object>
        {
            { "spender", request.Spender },
            { "amount", request.Amount }
        };

        var receipt = _executor.ExecuteCall(contractId, request.Owner, "approve", parameters);

        if (receipt == null)
            return BadRequest("Approve failed");

        return Ok(new
        {
            success = receipt.IsSuccess,
            transactionHash = receipt.TransactionHash,
            owner = request.Owner,
            spender = request.Spender,
            amount = request.Amount
        });
    }

    /// <summary>
    /// Get balance din contract
    /// </summary>
    [HttpGet("{contractId}/balance/{account}")]
    public IActionResult GetBalance(string contractId, string account)
    {
        var parameters = new Dictionary<string, object> { { "account", account } };
        var receipt = _executor.ExecuteCall(contractId, account, "balanceOf", parameters);

        if (receipt == null || !receipt.IsSuccess)
            return BadRequest("Failed to get balance");

        return Ok(new
        {
            account = account,
            balance = receipt.Output
        });
    }

    /// <summary>
    /// Start AI Training via contract
    /// </summary>
    [HttpPost("{contractId}/ai-training/start")]
    public IActionResult StartAITraining(string contractId, [FromBody] StartAITrainingRequest request)
    {
        var parameters = new Dictionary<string, object>
        {
            { "modelId", request.ModelId },
            { "epochs", request.Epochs }
        };

        var receipt = _executor.ExecuteCall(contractId, request.UserAddress, "startTraining", parameters, request.Payment);

        if (receipt == null || !receipt.IsSuccess)
            return BadRequest("Failed to start training");

        return Ok(new
        {
            success = true,
            transactionHash = receipt.TransactionHash,
            jobId = receipt.Output,
            modelId = request.ModelId,
            epochs = request.Epochs,
            payment = request.Payment
        });
    }

    /// <summary>
    /// Stake tokeni via contract
    /// </summary>
    [HttpPost("{contractId}/staking/stake")]
    public IActionResult Stake(string contractId, [FromBody] StakingRequest request)
    {
        var parameters = new Dictionary<string, object> { { "amount", request.Amount } };
        var receipt = _executor.ExecuteCall(contractId, request.UserAddress, "stake", parameters, request.Amount);

        if (receipt == null || !receipt.IsSuccess)
            return BadRequest("Staking failed");

        return Ok(new
        {
            success = true,
            transactionHash = receipt.TransactionHash,
            userAddress = request.UserAddress,
            amount = request.Amount,
            gasUsed = receipt.GasUsed
        });
    }

    /// <summary>
    /// Unstake tokeni via contract
    /// </summary>
    [HttpPost("{contractId}/staking/unstake")]
    public IActionResult Unstake(string contractId, [FromBody] StakingRequest request)
    {
        var parameters = new Dictionary<string, object> { { "amount", request.Amount } };
        var receipt = _executor.ExecuteCall(contractId, request.UserAddress, "unstake", parameters);

        if (receipt == null || !receipt.IsSuccess)
            return BadRequest("Unstaking failed");

        return Ok(new
        {
            success = true,
            transactionHash = receipt.TransactionHash,
            userAddress = request.UserAddress,
            amount = request.Amount,
            output = receipt.Output
        });
    }

    /// <summary>
    /// Obtine call history
    /// </summary>
    [HttpGet("{contractId}/history")]
    public IActionResult GetCallHistory(string contractId)
    {
        var calls = _executor.GetCallHistory(contractId);
        var result = calls.Select(c => new
        {
            callId = c.CallId,
            function = c.FunctionName,
            caller = c.CallerAddress,
            gasUsed = c.GasUsed,
            status = c.Status.ToString(),
            timestamp = c.Timestamp,
            returnValue = c.ReturnValue
        });

        return Ok(result);
    }

    /// <summary>
    /// Obtie receipts
    /// </summary>
    [HttpGet("{contractId}/receipts")]
    public IActionResult GetReceipts(string contractId)
    {
        var receipts = _executor.GetReceipts(contractId);
        var result = receipts.Select(r => new
        {
            transactionHash = r.TransactionHash,
            gasUsed = r.GasUsed,
            isSuccess = r.IsSuccess,
            output = r.Output,
            timestamp = r.Timestamp
        });

        return Ok(result);
    }

    /// <summary>
    /// Lista toti contractele
    /// </summary>
    [HttpGet("list/all")]
    public IActionResult ListAllContracts()
    {
        var contracts = _executor.GetAllContracts();
        var result = contracts.Select(c => new
        {
            contractId = c.ContractId,
            address = c.Address,
            name = c.Name,
            type = c.Type.ToString(),
            balance = c.Balance,
            deployedAt = c.DeployedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Obtie statistici
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        var stats = _executor.GetStatistics();
        return Ok(stats);
    }

    /// <summary>
    /// Obtie starea contractului
    /// </summary>
    [HttpGet("{contractId}/state/{key}")]
    public IActionResult GetState(string contractId, string key)
    {
        var contract = _executor.GetContract(contractId);
        if (contract == null)
            return NotFound("Contract not found");

        var value = contract.GetStateValue(key);
        if (value == null)
            return NotFound("State key not found");

        return Ok(new { key = key, value = value });
    }

    /// <summary>
    /// Obtie evenimentele contractului
    /// </summary>
    [HttpGet("{contractId}/events")]
    public IActionResult GetEvents(string contractId)
    {
        var contract = _executor.GetContract(contractId);
        if (contract == null)
            return NotFound("Contract not found");

        var result = contract.Events.Select(e => new
        {
            eventId = e.EventId,
            name = e.Name,
            timestamp = e.Timestamp,
            data = e.Data
        });

        return Ok(result);
    }
}

// Request DTOs
public class DeployContractRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string CreatorAddress { get; set; } = "";
    public string Bytecode { get; set; } = "";
    public string ABI { get; set; } = "";
}

public class ExecuteCallRequest
{
    public string CallerAddress { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public Dictionary<string, object>? Parameters { get; set; }
    public decimal Value { get; set; } = 0;
    public long GasLimit { get; set; } = 21000;
}

public class TransferRequest
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public decimal Amount { get; set; }
}

public class ApproveRequest
{
    public string Owner { get; set; } = "";
    public string Spender { get; set; } = "";
    public decimal Amount { get; set; }
}

public class StartAITrainingRequest
{
    public string UserAddress { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int Epochs { get; set; }
    public decimal Payment { get; set; }
}

public class StakingRequest
{
    public string UserAddress { get; set; } = "";
    public decimal Amount { get; set; }
}
