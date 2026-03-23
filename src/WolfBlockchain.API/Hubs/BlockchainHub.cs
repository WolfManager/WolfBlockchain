using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WolfBlockchain.API.Hubs;

/// <summary>
/// Real-time updates hub for blockchain events (blocks, transactions, network stats).
/// Clients connect via SignalR to receive live dashboard updates.
/// </summary>
public class BlockchainHub : Hub
{
    private readonly ILogger<BlockchainHub> _logger;

    public BlockchainHub(ILogger<BlockchainHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time blockchain updates.
    /// </summary>
    public async Task SubscribeToUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "blockchain-updates");
        _logger.LogInformation($"Client {Context.ConnectionId} subscribed to blockchain updates");
    }

    /// <summary>
    /// Unsubscribe from blockchain updates.
    /// </summary>
    public async Task UnsubscribeFromUpdates()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "blockchain-updates");
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from blockchain updates");
    }
}

/// <summary>
/// DTO for real-time blockchain event (block added, transaction confirmed, etc).
/// </summary>
public record BlockchainEventDto(
    string EventType,
    string? BlockHash,
    int? TransactionCount,
    DateTime Timestamp,
    string? Message
);

/// <summary>
/// DTO for real-time network statistics update.
/// </summary>
public record NetworkStatsDto(
    long TotalBlocks,
    long TotalTransactions,
    int ActiveNodes,
    decimal NetworkHashRate,
    decimal Difficulty,
    DateTime UpdatedAt
);
