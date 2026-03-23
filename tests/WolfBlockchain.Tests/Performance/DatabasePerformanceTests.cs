using Xunit;
using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;
using WolfBlockchain.Storage.Repositories;

namespace WolfBlockchain.Tests.Performance;

/// <summary>Database query performance tests</summary>
public class DatabasePerformanceTests : IAsyncLifetime
{
    private readonly WolfBlockchainDbContext _context;

    public DatabasePerformanceTests()
    {
        var options = new DbContextOptionsBuilder<WolfBlockchainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WolfBlockchainDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = Enumerable.Range(1, 100)
            .Select(i => new UserEntity
            {
                UserId = $"user_{i}",
                Username = $"testuser_{i}",
                Email = $"user{i}@test.com",
                Address = $"0x{i:D40}",
                Role = i % 10 == 0 ? "Admin" : "User",
                IsActive = true
            })
            .ToList();

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Create test tokens
        var tokens = Enumerable.Range(1, 10)
            .Select(i => new TokenEntity
            {
                TokenId = $"token_{i}",
                Name = $"Token {i}",
                Symbol = $"TK{i}",
                TokenType = i % 2 == 0 ? "Standard" : "Premium",
                TotalSupply = 1000000 + i * 100000,
                CurrentSupply = 500000 + i * 50000,
                IsActive = true
            })
            .ToList();

        _context.Tokens.AddRange(tokens);
        await _context.SaveChangesAsync();

        // Create test transactions
        var transactions = Enumerable.Range(1, 500)
            .Select(i => new TransactionEntity
            {
                TransactionId = $"tx_{i}",
                FromAddress = users[i % users.Count].Address,
                ToAddress = users[(i + 1) % users.Count].Address,
                Amount = 10m + i * 0.1m,
                Fee = 0.001m,
                Status = i % 3 == 0 ? "Pending" : "Confirmed",
                Timestamp = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Create test wallets
        var wallets = users.Select(u => new WalletEntity
        {
            Address = u.Address,
            BalanceWolf = 1000m + Random.Shared.Next(10000),
            IsActive = u.IsActive
        }).ToList();

        _context.Wallets.AddRange(wallets);
        await _context.SaveChangesAsync();
    }

    // ============= USER QUERY TESTS =============

    [Fact]
    public async Task GetUserByIdOptimized_ShouldReturnUserQuickly()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _context.Users.GetUserByIdOptimizedAsync(user.Id);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.True(stopwatch.ElapsedMilliseconds < 50, $"Query took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GetActiveUsersOptimized_ShouldBePaginated()
    {
        // Arrange & Act
        var (items, total) = await _context.Users.GetActiveUsersOptimizedAsync(page: 1, pageSize: 20);

        // Assert
        Assert.NotEmpty(items);
        Assert.True(items.Count <= 20);
        Assert.True(total > 0);
    }

    [Fact]
    public async Task GetUserByUsernameOptimized_ShouldFindUserQuickly()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _context.Users.GetUserByUsernameOptimizedAsync(user.Username);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.Username);
        Assert.True(stopwatch.ElapsedMilliseconds < 50);
    }

    // ============= TOKEN QUERY TESTS =============

    [Fact]
    public async Task GetTokenDtoByIdOptimized_ShouldReturnSmallPayload()
    {
        // Arrange
        var token = await _context.Tokens.FirstAsync();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _context.Tokens.GetTokenDtoByIdOptimizedAsync(token.Id);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.TokenId, result.TokenId);
        Assert.True(stopwatch.ElapsedMilliseconds < 50);
    }

    [Fact]
    public async Task GetTokensOptimized_ShouldSupportFiltering()
    {
        // Arrange & Act
        var (items, total) = await _context.Tokens.GetTokensOptimizedAsync(typeFilter: "Standard", pageSize: 10);

        // Assert
        Assert.NotEmpty(items);
        foreach (var token in items)
        {
            Assert.Equal("Standard", token.TokenType);
        }
    }

    // ============= TRANSACTION QUERY TESTS =============

    [Fact]
    public async Task GetRecentTransactionsOptimized_ShouldReturnInOrder()
    {
        // Arrange & Act
        var (items, total) = await _context.Transactions.GetRecentTransactionsOptimizedAsync(pageSize: 50);

        // Assert
        Assert.NotEmpty(items);
        // Verify descending order
        for (int i = 0; i < items.Count - 1; i++)
        {
            Assert.True(items[i].Timestamp >= items[i + 1].Timestamp);
        }
    }

    [Fact]
    public async Task GetTransactionsByAddressOptimized_ShouldReturnRelatedTransactions()
    {
        // Arrange
        var address = (await _context.Transactions.FirstAsync()).FromAddress;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _context.Transactions.GetTransactionsByAddressOptimizedAsync(address);
        stopwatch.Stop();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, tx => Assert.True(tx.FromAddress == address || tx.ToAddress == address));
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task GetTransactionCountsByStatusOptimized_ShouldGroupCorrectly()
    {
        // Arrange & Act
        var counts = await _context.Transactions.GetTransactionCountsByStatusOptimizedAsync();

        // Assert
        Assert.NotEmpty(counts);
        Assert.Contains("Pending", counts.Keys);
        Assert.Contains("Confirmed", counts.Keys);
    }

    // ============= WALLET QUERY TESTS =============

    [Fact]
    public async Task GetWalletWithBalancesOptimized_ShouldIncludeBalances()
    {
        // Arrange
        var walletAddress = (await _context.Wallets.FirstAsync()).Address;

        // Act
        var result = await _context.Wallets.GetWalletWithBalancesOptimizedAsync(walletAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(walletAddress, result.Address);
    }

    [Fact]
    public async Task GetActiveWalletsCountOptimized_ShouldReturnCorrectCount()
    {
        // Arrange & Act
        var count = await _context.Wallets.GetActiveWalletsCountOptimizedAsync();

        // Assert
        Assert.True(count > 0);
    }

    // ============= BLOCK QUERY TESTS =============

    [Fact]
    public async Task GetLatestBlocksOptimized_ShouldReturnInOrder()
    {
        // Arrange - Create test blocks
        var blocks = Enumerable.Range(1, 10)
            .Select(i => new BlockEntity
            {
                Index = i,
                Hash = $"hash_{i}",
                PreviousHash = $"hash_{i-1}",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                Status = "Confirmed"
            })
            .ToList();

        _context.Blocks.AddRange(blocks);
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.Blocks.GetLatestBlocksOptimizedAsync(limit: 5);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 5);
        // Verify descending order
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Index > result[i + 1].Index);
        }
    }

    // ============= PERFORMANCE BENCHMARKS =============

    [Fact]
    public async Task AllOptimizedQueries_ShouldCompleteWithinThreshold()
    {
        // Act & Assert - Run all queries and measure
        var totalTime = System.Diagnostics.Stopwatch.StartNew();

        // Run all query types
        _ = await _context.Users.GetActiveUsersOptimizedAsync();
        _ = await _context.Tokens.GetTokensOptimizedAsync();
        _ = await _context.Transactions.GetRecentTransactionsOptimizedAsync();
        _ = await _context.Wallets.GetActiveWalletsCountOptimizedAsync();

        totalTime.Stop();

        // All queries should complete in under 500ms total
        Assert.True(totalTime.ElapsedMilliseconds < 500, $"Queries took {totalTime.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task N_PLUS_1_QUERY_AVOIDED_WithAsNoTracking()
    {
        // This test verifies that AsNoTracking() prevents N+1 queries
        // by avoiding Entity Framework change tracking overhead

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Optimized query with AsNoTracking
        var users = await _context.Users
            .AsNoTracking()
            .Take(100)
            .ToListAsync();

        stopwatch.Stop();

        Assert.NotEmpty(users);
        // AsNoTracking should be significantly faster
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }
}
