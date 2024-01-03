using System.Net;

namespace RedisRateLimiting.Tests.Common;

public sealed class RateLimitResponse
{
    public HttpStatusCode StatusCode { get; set; }

    public long? Limit { get; set; }

    public long? Remaining { get; set; }

    public int? RetryAfter { get; set; }
}

