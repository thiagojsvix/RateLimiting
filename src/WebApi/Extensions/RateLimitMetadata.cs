using System.Threading.RateLimiting;

namespace WebApi.Extensions;

public static class RateLimitMetadata
{
    /// <summary>
    /// Sets the default rate limit headers.
    /// </summary>
    public static Func<HttpContext, RateLimitLease, CancellationToken, ValueTask> OnRejected { get; } = (httpContext, lease, token) =>
    {
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (lease.TryGetMetadata(RateLimitMetadataName.Limit, out var limit))
        {
            httpContext.Response.Headers[RateLimitHeaders.Limit] = limit;
        }

        if (lease.TryGetMetadata(RateLimitMetadataName.Remaining, out var remaining))
        {
            httpContext.Response.Headers[RateLimitHeaders.Remaining] = remaining.ToString();
        }

        if (lease.TryGetMetadata(RateLimitMetadataName.Reset, out var reset))
        {
            httpContext.Response.Headers[RateLimitHeaders.Reset] = reset.ToString();
        }

        if (lease.TryGetMetadata(RateLimitMetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers[RateLimitHeaders.RetryAfter] = retryAfter.ToString();
        }

        return ValueTask.CompletedTask;
    };
}

/// <summary>
/// Contains some common rate limiting metadata name-type pairs and helper method to create a metadata name.
/// </summary>
public static class RateLimitMetadataName
{
    /// <summary>
    /// Indicates how long the user agent should wait before making a follow-up request (in seconds).
    /// For example, used in <see cref="RedisFixedWindowRateLimiter{TKey}"/>.
    /// </summary>
    public static MetadataName<int> RetryAfter { get; } = MetadataName.Create<int>("RATELIMIT_RETRYAFTER");

    /// <summary>
    /// Request limit. For example, used in <see cref="RedisConcurrencyRateLimiter{TKey}"/>.
    /// Request limit per timespan. For example 100/30m, used in <see cref="RedisFixedWindowRateLimiter{TKey}"/>.
    /// </summary>
    public static MetadataName<string> Limit { get; } = MetadataName.Create<string>("RATELIMIT_LIMIT");

    /// <summary>
    /// The number of requests left for the time window.
    /// For example, used in <see cref="RedisConcurrencyRateLimiter{TKey}"/>.
    /// </summary>
    public static MetadataName<long> Remaining { get; } = MetadataName.Create<long>("RATELIMIT_REMAINING");

    /// <summary>
    /// The remaining window before the rate limit resets in seconds.
    /// For example, used in <see cref="RedisFixedWindowRateLimiter{TKey}"/>.
    /// </summary>
    public static MetadataName<long> Reset { get; } = MetadataName.Create<long>("RATELIMIT_RESET");
}

public static class RateLimitHeaders
{
    public const string Limit = "X-RateLimit-Limit";
    public const string Remaining = "X-RateLimit-Remaining";
    public const string Reset = "X-RateLimit-Reset";
    public const string RetryAfter = "Retry-After";
}
