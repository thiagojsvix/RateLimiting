using RedisRateLimiting.AspNetCore;

namespace RedisRateLimiting.Tests.Common;

public sealed class Request(HttpClient httpClient, string apiPath)
{
    public async Task<RateLimitResponse> MakeAsync(string pathRequest)
    {
        using var response = await httpClient.GetAsync(pathRequest);

        var rateLimitResponse = new RateLimitResponse
        {
            StatusCode = response.StatusCode,
        };

        if (response.Headers.TryGetValues(RateLimitHeaders.Limit, out var valuesLimit)
            && long.TryParse(valuesLimit.FirstOrDefault(), out var limit))
        {
            rateLimitResponse.Limit = limit;
        }

        if (response.Headers.TryGetValues(RateLimitHeaders.Remaining, out var valuesRemaining)
            && long.TryParse(valuesRemaining.FirstOrDefault(), out var remaining))
        {
            rateLimitResponse.Remaining = remaining;
        }

        if (response.Headers.TryGetValues(RateLimitHeaders.RetryAfter, out var valuesRetryAfter)
            && int.TryParse(valuesRetryAfter.FirstOrDefault(), out var retryAfter))
        {
            rateLimitResponse.RetryAfter = retryAfter;
        }

        return rateLimitResponse;
    }

    public async Task<RateLimitResponse> MakeAsync() => await MakeAsync(apiPath);

    public void AddHeader(string name, string value)
    {
        httpClient.DefaultRequestHeaders.Add(name, value);
    }
}
