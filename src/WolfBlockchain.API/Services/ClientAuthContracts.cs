namespace WolfBlockchain.API.Services;

internal sealed record OwnerLoginRequest(string Address, string Password);

internal sealed record OwnerLoginResponse
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
}
