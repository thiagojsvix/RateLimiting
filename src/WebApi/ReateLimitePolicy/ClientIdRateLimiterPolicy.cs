using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

using WebApi.Database;

namespace WebApi.ReateLimitePolicy;

public class ClientIdRateLimiterPolicy(IServiceProvider _serviceProvider, ILogger<ClientIdRateLimiterPolicy> logger) : IRateLimiterPolicy<string>
{

    public const string PolicyName = "demo_client_id";

    private readonly Func<OnRejectedContext, CancellationToken, ValueTask> _onRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        logger.LogWarning($"Request rejected by {nameof(ClientIdRateLimiterPolicy)}");

        return ValueTask.CompletedTask;
    };

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => _onRejected;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var clientId = httpContext.Request.Headers["X-ClientId"].ToString();

        using var scope = _serviceProvider.CreateScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<RateLimiteDbContext>();
        var rateLimit = dbContext.Clients.Where(x => x.Identifier == clientId).Select(x => x.RateLimit).FirstOrDefault();

        var permitLimit = rateLimit?.PermitLimit ?? 1;

        logger.LogInformation("Client: {clientId} PermitLimit: {permitLimit}", clientId, permitLimit);

        return RateLimitPartition.GetConcurrencyLimiter(clientId, key => new ConcurrencyLimiterOptions
        {
            PermitLimit = permitLimit,
            QueueLimit = rateLimit?.QueueLimit ?? 0,
        });
    }
}
