using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using WolfBlockchain.Agents.Abstractions;
using WolfBlockchain.Agents.Gateway;
using WolfBlockchain.Agents.Logging;
using WolfBlockchain.Agents.Memory;
using WolfBlockchain.Agents.Orchestration;
using WolfBlockchain.Agents.Policies;
using WolfBlockchain.Agents.Providers;
using WolfBlockchain.Agents.Services;

namespace WolfBlockchain.Tests.Agents;

/// <summary>Unit tests for the Copilot Coding Agent components, verifying independence from Ollama.</summary>
public class CopilotAgentTests
{
    // ============= COPILOT PROVIDER ADAPTER =============

    [Fact]
    public async Task CopilotProvider_GenerateCode_ReturnsCodeGenerationResponse()
    {
        var adapter = new CopilotProviderAdapter();
        var result = await adapter.GenerateAsync("1.0", "generate code for a wallet service", CancellationToken.None);
        Assert.NotEmpty(result);
        Assert.Contains("CopilotAgent", result);
        Assert.Contains("generation", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopilotProvider_AnalyzeCode_ReturnsAnalysisResponse()
    {
        var adapter = new CopilotProviderAdapter();
        var result = await adapter.GenerateAsync("1.0", "analyze code quality of this method", CancellationToken.None);
        Assert.NotEmpty(result);
        Assert.Contains("analysis", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopilotProvider_DebugCode_ReturnsDebuggingResponse()
    {
        var adapter = new CopilotProviderAdapter();
        var result = await adapter.GenerateAsync("1.0", "debug this error: NullReferenceException", CancellationToken.None);
        Assert.NotEmpty(result);
        Assert.Contains("Debugging", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopilotProvider_ArchitectureRequest_ReturnsArchitectureResponse()
    {
        var adapter = new CopilotProviderAdapter();
        var result = await adapter.GenerateAsync("1.0", "architecture design for microservices", CancellationToken.None);
        Assert.NotEmpty(result);
        Assert.Contains("Architecture", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CopilotProvider_ProviderName_IsNotOllama()
    {
        var adapter = new CopilotProviderAdapter();
        Assert.NotEqual("ollama", adapter.ProviderName, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("copilot-coding-agent", adapter.ProviderName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CopilotProvider_EmptyPrompt_ThrowsArgumentException(string prompt)
    {
        var adapter = new CopilotProviderAdapter();
        await Assert.ThrowsAsync<ArgumentException>(() => adapter.GenerateAsync("1.0", prompt, CancellationToken.None).AsTask());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CopilotProvider_EmptyPromptVersion_ThrowsArgumentException(string version)
    {
        var adapter = new CopilotProviderAdapter();
        await Assert.ThrowsAsync<ArgumentException>(() => adapter.GenerateAsync(version, "some prompt", CancellationToken.None).AsTask());
    }

    // ============= NULL PROVIDER ADAPTER =============

    [Fact]
    public async Task NullProvider_ReturnsNotConfiguredResponse()
    {
        var adapter = new NullProviderAdapter();
        var result = await adapter.GenerateAsync("1.0", "any prompt", CancellationToken.None);
        Assert.Equal("provider-not-configured", result);
    }

    [Fact]
    public void NullProvider_ProviderName_IsNull()
    {
        var adapter = new NullProviderAdapter();
        Assert.Equal("null", adapter.ProviderName);
    }

    // ============= POLICY ENGINE =============

    [Fact]
    public async Task PolicyEngine_DeniesUnapprovedTransactionSubmission()
    {
        var engine = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction, new Dictionary<string, string>());
        var result = await engine.EvaluateAsync(request, new AgentPolicyContext("test", new[] { "agent-runtime" }, "req-1"), CancellationToken.None);
        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task PolicyEngine_AllowsReadChainData()
    {
        var engine = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("agent-1", AgentActionType.ReadChainData, new Dictionary<string, string>());
        var result = await engine.EvaluateAsync(request, new AgentPolicyContext("test", new[] { "agent-runtime" }, "req-1"), CancellationToken.None);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task PolicyEngine_AllowsAuthorizedTransactionSubmission()
    {
        var engine = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction,
            new Dictionary<string, string> { ["authorizedCommand"] = "true" });
        var result = await engine.EvaluateAsync(request, new AgentPolicyContext("test", new[] { "agent-runtime" }, "req-1"), CancellationToken.None);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task PolicyEngine_DeniesRequestWithMissingAgentId()
    {
        var engine = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("", AgentActionType.ReadChainData, new Dictionary<string, string>());
        var result = await engine.EvaluateAsync(request, new AgentPolicyContext("test", new[] { "agent-runtime" }, "req-1"), CancellationToken.None);
        Assert.False(result.Allowed);
        Assert.Equal("missing-agent-id", result.Outcome);
    }

    // ============= ORCHESTRATOR =============

    [Fact]
    public async Task Orchestrator_AllowsAuthorizedGatewayAction()
    {
        var loggerMock = new Mock<ILogger<StructuredAuditLogger>>();
        var orchestrator = new SafeAgentOrchestrator(
            new DefaultAgentPolicyEngine(),
            new SafeAgentActionGateway(),
            new StructuredAuditLogger(loggerMock.Object));

        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction,
            new Dictionary<string, string> { ["authorizedCommand"] = "true" });

        var result = await orchestrator.ExecuteAsync(request, CancellationToken.None);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task Orchestrator_BlocksUnauthorizedTransactionSubmission()
    {
        var loggerMock = new Mock<ILogger<StructuredAuditLogger>>();
        var orchestrator = new SafeAgentOrchestrator(
            new DefaultAgentPolicyEngine(),
            new SafeAgentActionGateway(),
            new StructuredAuditLogger(loggerMock.Object));

        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction, new Dictionary<string, string>());
        var result = await orchestrator.ExecuteAsync(request, CancellationToken.None);
        Assert.False(result.Allowed);
    }

    // ============= MEMORY STORE =============

    [Fact]
    public void MemoryStore_SaveAndGet_ReturnsStoredValue()
    {
        var store = new InMemoryAgentMemoryStore();
        store.Save("key1", "value1");
        Assert.Equal("value1", store.Get("key1"));
    }

    [Fact]
    public void MemoryStore_GetMissingKey_ReturnsNull()
    {
        var store = new InMemoryAgentMemoryStore();
        Assert.Null(store.Get("missing"));
    }

    // ============= CODING AGENT SERVICE =============

    [Fact]
    public async Task CodingAgentService_GenerateCode_ReturnsSuccessfulResponse()
    {
        var service = new CopilotCodingAgentService(new CopilotProviderAdapter(), new InMemoryAgentMemoryStore());
        var response = await service.GenerateCodeAsync("create a blockchain transaction");
        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
        Assert.Equal("copilot-coding-agent", response.ProviderName);
    }

    [Fact]
    public async Task CodingAgentService_AnalyzeCode_ReturnsSuccessfulResponse()
    {
        var service = new CopilotCodingAgentService(new CopilotProviderAdapter(), new InMemoryAgentMemoryStore());
        var response = await service.AnalyzeCodeAsync("public void Foo() { }");
        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
    }

    [Fact]
    public async Task CodingAgentService_DebugCode_ReturnsSuccessfulResponse()
    {
        var service = new CopilotCodingAgentService(new CopilotProviderAdapter(), new InMemoryAgentMemoryStore());
        var response = await service.DebugCodeAsync("int x = null;", "NullReferenceException on x");
        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
    }

    [Fact]
    public async Task CodingAgentService_AdviseArchitecture_ReturnsSuccessfulResponse()
    {
        var service = new CopilotCodingAgentService(new CopilotProviderAdapter(), new InMemoryAgentMemoryStore());
        var response = await service.AdviseArchitectureAsync("design a distributed blockchain node");
        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
    }

    [Fact]
    public void CodingAgentService_ProviderName_IsNotOllama()
    {
        var service = new CopilotCodingAgentService(new CopilotProviderAdapter(), new InMemoryAgentMemoryStore());
        Assert.NotEqual("ollama", service.ProviderName, StringComparer.OrdinalIgnoreCase);
    }
}
