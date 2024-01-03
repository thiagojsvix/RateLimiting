using System.Net;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

using WebApi.Model;

namespace WebApi.Middlewares;

public class ConcurrencyRateLimiterPolicy(ILogger<ConcurrencyRateLimiterPolicy> logger,
                                          IOptions<RateLimiterConfiguration> configuration) : IRateLimiterPolicy<IPAddress>
{
    public const string PolicyName = "Concurrency";
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

    public RateLimitPartition<IPAddress> GetPartition(HttpContext httpContext)
    {
        IPAddress remoteIpAddress = httpContext.Connection.RemoteIpAddress;

        if (!IPAddress.IsLoopback(remoteIpAddress!))
        {
            return RateLimitPartition.GetConcurrencyLimiter(remoteIpAddress!, _ =>
            new ConcurrencyLimiterOptions
            {
                PermitLimit = _configuration.PermitLimit * 2,
                QueueLimit = _configuration.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        }

        return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);
    }
}
