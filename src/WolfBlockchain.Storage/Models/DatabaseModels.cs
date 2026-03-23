using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WolfBlockchain.Storage.Models;

[Table("Blocks")]
public class BlockEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int Index { get; set; }
    public DateTime Timestamp { get; set; }
    [MaxLength(256)]
    public string PreviousHash { get; set; } = "";
    [MaxLength(256)]
    public string Hash { get; set; } = "";
    public int Nonce { get; set; }
    [MaxLength(256)]
    public string Validator { get; set; } = "";
    public int Difficulty { get; set; }
    [MaxLength(50)]
    public string Status { get; set; } = "Valid";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [InverseProperty(nameof(TransactionEntity.Block))]
    public virtual ICollection<TransactionEntity> Transactions { get; set; } = new List<TransactionEntity>();
}

[Table("Transactions")]
public class TransactionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(256)]
    public string TransactionId { get; set; } = "";
    [MaxLength(256)]
    public string FromAddress { get; set; } = "";
    [MaxLength(256)]
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    [MaxLength(256)]
    public string Hash { get; set; } = "";
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    [MaxLength(500)]
    public string? Description { get; set; }
    public int? BlockId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(BlockId))]
    public virtual BlockEntity? Block { get; set; }
}

[Table("Wallets")]
public class WalletEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(256)]
    public string Address { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string PrivateKeyEncrypted { get; set; } = "";
    public decimal BalanceWolf { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    [InverseProperty(nameof(UserEntity.Wallet))]
    public virtual UserEntity? User { get; set; }
    [InverseProperty(nameof(TokenBalanceEntity.Wallet))]
    public virtual ICollection<TokenBalanceEntity> TokenBalances { get; set; } = new List<TokenBalanceEntity>();
}

[Table("Users")]
public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(256)]
    public string UserId { get; set; } = "";
    [MaxLength(256)]
    public string Username { get; set; } = "";
    [MaxLength(256)]
    public string Address { get; set; } = "";
    [MaxLength(50)]
    public string Role { get; set; } = "User";
    public long Permissions { get; set; } = 0;
    public string PasswordHash { get; set; } = "";
    public bool TwoFactorEnabled { get; set; } = false;
    public bool IsActive { get; set; } = true;
    [MaxLength(256)]
    public string? Email { get; set; }
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(Address))]
    public virtual WalletEntity? Wallet { get; set; }
}

[Table("Tokens")]
public class TokenEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(256)]
    public string TokenId { get; set; } = "";
    [MaxLength(256)]
    public string Name { get; set; } = "";
    [MaxLength(10)]
    public string Symbol { get; set; } = "";
    [MaxLength(50)]
    public string TokenType { get; set; } = "Custom";
    [MaxLength(256)]
    public string CreatorAddress { get; set; } = "";
    public decimal TotalSupply { get; set; }
    public decimal CurrentSupply { get; set; }
    public int Decimals { get; set; } = 18;
    public bool IsActive { get; set; } = true;
    [MaxLength(1000)]
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [InverseProperty(nameof(TokenBalanceEntity.Token))]
    public virtual ICollection<TokenBalanceEntity> TokenBalances { get; set; } = new List<TokenBalanceEntity>();
    [InverseProperty(nameof(TokenTransactionEntity.Token))]
    public virtual ICollection<TokenTransactionEntity> TokenTransactions { get; set; } = new List<TokenTransactionEntity>();
}

[Table("TokenTransactions")]
public class TokenTransactionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(256)]
    public string TransactionId { get; set; } = "";
    public int TokenId { get; set; }
    [MaxLength(256)]
    public string FromAddress { get; set; } = "";
    [MaxLength(256)]
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    [MaxLength(256)]
    public string Hash { get; set; } = "";
    public DateTime Timestamp { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(TokenId))]
    public virtual TokenEntity? Token { get; set; }
}

[Table("TokenBalances")]
public class TokenBalanceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int WalletId { get; set; }
    public int TokenId { get; set; }
    public decimal Balance { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(WalletId))]
    public virtual WalletEntity? Wallet { get; set; }
    [ForeignKey(nameof(TokenId))]
    public virtual TokenEntity? Token { get; set; }
}
