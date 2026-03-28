using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Provides RPC connectivity checks with primary/fallback failover.
/// </summary>
public interface IRpcFailoverService
{
    Task<RpcProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
}

public sealed record RpcProbeResult(bool IsHealthy, string? ActiveEndpointHost, bool UsedFallback, string? Error);

public sealed class RpcFailoverOptions
{
    public string? PrimaryEndpoint { get; set; }
    public string? FallbackEndpoint { get; set; }
    public string? AuthToken { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 2;
    public int BackoffMs { get; set; } = 250;
}

/// <summary>
/// Checks primary RPC endpoint first and falls back to secondary endpoint on timeout/failure.
/// </summary>
public sealed class RpcFailoverService : IRpcFailoverService
{
    private readonly HttpClient _httpClient;
    private readonly RpcFailoverOptions _options;
    private readonly ILogger<RpcFailoverService> _logger;

    public RpcFailoverService(HttpClient httpClient, IOptions<RpcFailoverOptions> options, ILogger<RpcFailoverService> logger)
    {
        _httpClient = httpClient;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RpcProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var primary = ParseEndpoint(_options.PrimaryEndpoint);
        var fallback = ParseEndpoint(_options.FallbackEndpoint);

        if (primary is null && fallback is null)
        {
            return new RpcProbeResult(false, null, false, "No RPC endpoint configured.");
        }

        if (primary is not null)
        {
            var primaryResult = await ProbeEndpointWithRetryAsync(primary, false, cancellationToken).ConfigureAwait(false);
            if (primaryResult.IsHealthy)
            {
                return primaryResult;
            }

            if (fallback is null)
            {
                return primaryResult;
            }

            _logger.LogWarning("RPC primary endpoint failed; attempting fallback endpoint.");
        }

        if (fallback is not null)
        {
            var fallbackResult = await ProbeEndpointWithRetryAsync(fallback, true, cancellationToken).ConfigureAwait(false);
            if (fallbackResult.IsHealthy)
            {
                return fallbackResult;
            }

            return fallbackResult;
        }

        return new RpcProbeResult(false, null, false, "RPC probe failed.");
    }

    private async Task<RpcProbeResult> ProbeEndpointWithRetryAsync(Uri endpoint, bool usedFallback, CancellationToken cancellationToken)
    {
        var timeoutSeconds = Math.Max(1, _options.TimeoutSeconds);
        var retryCount = Math.Max(0, _options.RetryCount);
        var backoffMs = Math.Max(50, _options.BackoffMs);

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                if (!string.IsNullOrWhiteSpace(_options.AuthToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AuthToken);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return new RpcProbeResult(true, endpoint.Host, usedFallback, null);
                }

                _logger.LogWarning("RPC probe failed with status code {StatusCode} on host {Host}. Attempt {Attempt}/{MaxAttempts}.",
                    (int)response.StatusCode,
                    endpoint.Host,
                    attempt + 1,
                    retryCount + 1);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("RPC probe timed out on host {Host}. Attempt {Attempt}/{MaxAttempts}.",
                    endpoint.Host,
                    attempt + 1,
                    retryCount + 1);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "RPC probe request failed on host {Host}. Attempt {Attempt}/{MaxAttempts}.",
                    endpoint.Host,
                    attempt + 1,
                    retryCount + 1);
            }

            if (attempt < retryCount)
            {
                var delayMs = backoffMs * (attempt + 1);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return new RpcProbeResult(false, endpoint.Host, usedFallback, "RPC endpoint is unreachable.");
    }

    private static Uri? ParseEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return null;

        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri : null;
    }
}
