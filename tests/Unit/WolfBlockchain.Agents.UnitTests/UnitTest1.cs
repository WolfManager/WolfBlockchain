using WolfBlockchain.Agents.Abstractions;
using Xunit;

namespace WolfBlockchain.Agents.UnitTests;

public class AgentPolicyContractsTests
{
    [Fact]
    public void AgentActionRequest_ShouldKeepActionTypeExplicit()
    {
        var request = new AgentActionRequest(
            AgentId: "agent-1",
            ActionType: AgentActionType.SubmitTransaction,
            Parameters: new Dictionary<string, string> { ["tx"] = "payload" });

        Assert.Equal("agent-1", request.AgentId);
        Assert.Equal(AgentActionType.SubmitTransaction, request.ActionType);
        Assert.True(request.Parameters.ContainsKey("tx"));
    }

    [Fact]
    public void AgentPolicyEvaluator_ShouldReturnValueTaskBoolean()
    {
        var method = typeof(IAgentPolicyEvaluator).GetMethod(nameof(IAgentPolicyEvaluator.IsAllowedAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(ValueTask<bool>), method!.ReturnType);
    }

    [Fact]
    public void AgentActionResult_ShouldExposeExplicitOutcome()
    {
        var result = new AgentActionResult(Allowed: true, Outcome: "approved");

        Assert.True(result.Allowed);
        Assert.Equal("approved", result.Outcome);
    }
}
