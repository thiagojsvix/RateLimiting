using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

using WebApi.Model;

namespace WebApi.Middlewares;

public class JwtRateLimiterPolicy(ILogger<JwtRateLimiterPolicy> logger,
                                  IOptions<RateLimiterConfiguration> configuration) : IRateLimiterPolicy<string>
{
    public const string PolicyName = "jwt";

    private readonly RateLimiterConfiguration _configuration = configuration.Value;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask> _onRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            logger.LogWarning($"Request rejected by {nameof(JwtRateLimiterPolicy)}");

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                await context.HttpContext.Response.WriteAsync($"Limite de solicitações foi atingido. Tente novamente dentro de {retryAfter.TotalSeconds} segundos.");
            }
            else
            {
                await context.HttpContext.Response.WriteAsync("Limite de solicitações foi atingido. Tente novamente mais tarde.");
            }
        };

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => _onRejected;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var accessToken = httpContext.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Properties?.GetTokenValue("access_token")?.ToString() ?? string.Empty;

        return RateLimitPartition.GetTokenBucketLimiter(accessToken, _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = _configuration.TokenLimit,
                QueueLimit = _configuration.QueueLimit,
                TokensPerPeriod = _configuration.TokensPerPeriod,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = _configuration.AutoReplenishment,
                ReplenishmentPeriod = TimeSpan.FromSeconds(_configuration.ReplenishmentPeriod),
            });
    }
}
