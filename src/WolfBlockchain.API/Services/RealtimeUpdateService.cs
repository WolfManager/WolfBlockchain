using Microsoft.AspNetCore.SignalR;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Service to broadcast real-time blockchain events to connected SignalR clients.
/// Used by blockchain operations (block creation, transactions, etc) to push updates.
/// </summary>
public class RealtimeUpdateService
{
    private readonly IHubContext<Hubs.BlockchainHub> _hubContext;
    private readonly ILogger<RealtimeUpdateService> _logger;

    public RealtimeUpdateService(IHubContext<Hubs.BlockchainHub> hubContext, ILogger<RealtimeUpdateService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notify all connected clients of a new block.
    /// </summary>
    public async Task NotifyBlockAddedAsync(string blockHash, int transactionCount)
    {
        try
        {
            var @event = new Hubs.BlockchainEventDto(
                EventType: "BlockAdded",
                BlockHash: blockHash,
                TransactionCount: transactionCount,
                Timestamp: DateTime.UtcNow,
                Message: $"New block {blockHash[..8]}... with {transactionCount} transactions"
            );

            await _hubContext.Clients.Group("blockchain-updates").SendAsync("BlockAdded", @event);
            _logger.LogInformation($"Broadcast: Block added {blockHash[..8]}...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast block added event");
        }
    }

    /// <summary>
    /// Notify all connected clients of a confirmed transaction.
    /// </summary>
    public async Task NotifyTransactionConfirmedAsync(string txHash, decimal amount, string fromAddress, string toAddress)
    {
        try
        {
            var @event = new Hubs.BlockchainEventDto(
                EventType: "TransactionConfirmed",
                BlockHash: null,
                TransactionCount: 1,
                Timestamp: DateTime.UtcNow,
                Message: $"Transaction {amount:F2} WOLF from {fromAddress[..6]}... to {toAddress[..6]}..."
            );

            await _hubContext.Clients.Group("blockchain-updates").SendAsync("TransactionConfirmed", @event);
            _logger.LogInformation($"Broadcast: Transaction confirmed {txHash[..8]}...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast transaction confirmed event");
        }
    }

    /// <summary>
    /// Notify all connected clients of updated network statistics.
    /// </summary>
    public async Task NotifyNetworkStatsUpdatedAsync(
        long totalBlocks,
        long totalTransactions,
        int activeNodes,
        decimal networkHashRate,
        decimal difficulty)
    {
        try
        {
            var stats = new Hubs.NetworkStatsDto(
                TotalBlocks: totalBlocks,
                TotalTransactions: totalTransactions,
                ActiveNodes: activeNodes,
                NetworkHashRate: networkHashRate,
                Difficulty: difficulty,
                UpdatedAt: DateTime.UtcNow
            );

            await _hubContext.Clients.Group("blockchain-updates").SendAsync("NetworkStatsUpdated", stats);
            _logger.LogDebug($"Broadcast: Network stats updated (blocks={totalBlocks}, tx={totalTransactions})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast network stats update");
        }
    }

    /// <summary>
    /// Broadcast a generic event to all connected clients.
    /// </summary>
    public async Task BroadcastEventAsync(string eventName, object data)
    {
        try
        {
            await _hubContext.Clients.Group("blockchain-updates").SendAsync(eventName, data);
            _logger.LogDebug($"Broadcast event: {eventName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to broadcast event: {eventName}");
        }
    }
}
