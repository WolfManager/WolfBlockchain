using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace WolfBlockchain.API.Services;

internal sealed class ClientAuthService
{
    private const string AccessTokenKey = "wolf_access_token";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public ClientAuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public Task<string?> GetTokenAsync()
    {
        return _js.InvokeAsync<string?>("authStorage.getToken").AsTask();
    }

    public async Task SetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        await _js.InvokeVoidAsync("authStorage.setToken", token);
    }

    public Task ClearTokenAsync()
    {
        return _js.InvokeVoidAsync("authStorage.clearToken").AsTask();
    }

    public async Task<string?> GetValidTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (await IsTokenValidAsync(token))
            return token;

        await ClearTokenAsync();
        return null;
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var response = await _http.PostAsJsonAsync("/api/security/validate-token", new ValidateTokenRequest(token));
        if (!response.IsSuccessStatusCode)
            return false;

        var payload = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>();
        return payload?.IsValid == true;
    }

    private sealed record ValidateTokenRequest(string Token);

    private sealed record ValidateTokenResponse(bool IsValid);
}
