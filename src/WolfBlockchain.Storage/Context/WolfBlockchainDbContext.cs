using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.Storage.Context;

/// <summary>
/// Database Context pentru Wolf Blockchain
/// </summary>
public class WolfBlockchainDbContext : DbContext
{
    public WolfBlockchainDbContext(DbContextOptions<WolfBlockchainDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<BlockEntity> Blocks { get; set; } = null!;
    public DbSet<TransactionEntity> Transactions { get; set; } = null!;
    public DbSet<WalletEntity> Wallets { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<TokenEntity> Tokens { get; set; } = null!;
    public DbSet<TokenTransactionEntity> TokenTransactions { get; set; } = null!;
    public DbSet<TokenBalanceEntity> TokenBalances { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BlockEntity
        modelBuilder.Entity<BlockEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Index).IsUnique();
            entity.HasIndex(e => e.Hash).IsUnique();
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.PreviousHash).HasMaxLength(256);
            entity.Property(e => e.Hash).HasMaxLength(256);
            entity.Property(e => e.Validator).HasMaxLength(256);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.Block)
                .HasForeignKey(t => t.BlockId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TransactionEntity
        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.FromAddress);
            entity.HasIndex(e => e.ToAddress);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TransactionId).HasMaxLength(256);
            entity.Property(e => e.FromAddress).HasMaxLength(256);
            entity.Property(e => e.ToAddress).HasMaxLength(256);
            entity.Property(e => e.Hash).HasMaxLength(256);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(28, 18);
            entity.Property(e => e.Fee).HasPrecision(28, 18);

            entity.HasOne(e => e.Block)
                .WithMany(b => b.Transactions)
                .HasForeignKey(e => e.BlockId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure WalletEntity
        modelBuilder.Entity<WalletEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Address).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Address).HasMaxLength(256);
            entity.Property(e => e.BalanceWolf).HasPrecision(28, 18);

            entity.HasOne(e => e.User)
                .WithOne(u => u.Wallet)
                .HasPrincipalKey<WalletEntity>(w => w.Address)
                .HasForeignKey<UserEntity>(u => u.Address)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TokenBalances)
                .WithOne(tb => tb.Wallet)
                .HasForeignKey(tb => tb.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Address);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Role);

            entity.Property(e => e.UserId).HasMaxLength(256);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.Address).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        // Configure TokenEntity
        modelBuilder.Entity<TokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenId).IsUnique();
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.TokenType);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.TokenId).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.Symbol).HasMaxLength(10);
            entity.Property(e => e.TokenType).HasMaxLength(50);
            entity.Property(e => e.CreatorAddress).HasMaxLength(256);
            entity.Property(e => e.TotalSupply).HasPrecision(28, 18);
            entity.Property(e => e.CurrentSupply).HasPrecision(28, 18);

            entity.HasMany(e => e.TokenBalances)
                .WithOne(tb => tb.Token)
                .HasForeignKey(tb => tb.TokenId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.TokenTransactions)
                .WithOne(tt => tt.Token)
                .HasForeignKey(tt => tt.TokenId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TokenTransactionEntity
        modelBuilder.Entity<TokenTransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.FromAddress);
            entity.HasIndex(e => e.ToAddress);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TransactionId).HasMaxLength(256);
            entity.Property(e => e.FromAddress).HasMaxLength(256);
            entity.Property(e => e.ToAddress).HasMaxLength(256);
            entity.Property(e => e.Amount).HasPrecision(28, 18);
            entity.Property(e => e.Fee).HasPrecision(28, 18);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Hash).HasMaxLength(256);
        });

        // Configure TokenBalanceEntity
        modelBuilder.Entity<TokenBalanceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.WalletId, e.TokenId }).IsUnique();

            entity.Property(e => e.Balance).HasPrecision(28, 18);

            entity.HasOne(e => e.Wallet)
                .WithMany(w => w.TokenBalances)
                .HasForeignKey(e => e.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Token)
                .WithMany(t => t.TokenBalances)
                .HasForeignKey(e => e.TokenId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
