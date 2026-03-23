using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Api.AdminApi;

public sealed class AdminApiService(IAuditLogger auditLogger) : IAdminApiService
{
    public ValueTask<ApiResult<string>> GetNodeStatusAsync(ApiRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        auditLogger.Log(AuditEventType.ApiAccess, "admin.node-status.read", new Dictionary<string, string>
        {
            ["requestId"] = context.RequestId,
            ["callerId"] = context.CallerId
        });

        return ValueTask.FromResult(new ApiResult<string>(true, "NodeStatus: Healthy"));
    }

    public ValueTask<ApiResult<string>> GetConsensusStatusAsync(ApiRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        auditLogger.Log(AuditEventType.ApiAccess, "admin.consensus-status.read", new Dictionary<string, string>
        {
            ["requestId"] = context.RequestId,
            ["callerId"] = context.CallerId
        });

        return ValueTask.FromResult(new ApiResult<string>(true, "ConsensusStatus: Ready"));
    }
}
