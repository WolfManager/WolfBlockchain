namespace WolfBlockchain.API.Services.ClientAuthContracts;

/// <summary>
/// DTO for admin dashboard user data.
/// </summary>
public record UserDto(
    string UserId,
    string Username,
    string Address,
    string Role,
    decimal Balance,
    string Status,
    DateTime CreatedAt
);

/// <summary>
/// DTO for admin dashboard token data.
/// </summary>
public record TokenDto(
    string TokenId,
    string Name,
    string Symbol,
    string TokenType,
    long TotalSupply,
    long CurrentSupply,
    string Status,
    string CreatorAddress,
    DateTime CreatedAt
);

/// <summary>
/// DTO for admin dashboard summary statistics.
/// </summary>
public record AdminDashboardSummaryDto(
    int TotalUsers,
    int TotalTokens,
    int TotalValidators,
    long TotalStaked,
    int ActiveAITrainingJobs,
    int DeployedSmartContracts,
    DateTime LastUpdatedAt
);

/// <summary>
/// Paged list of users.
/// </summary>
public record PaginatedUsersDto(
    IEnumerable<UserDto> Users,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// Paged list of tokens.
/// </summary>
public record PaginatedTokensDto(
    IEnumerable<TokenDto> Tokens,
    int TotalCount,
    int Page,
    int PageSize
);
