using System.Net;

using Microsoft.AspNetCore.RateLimiting;

using RedisRateLimiting.AspNetCore;

using StackExchange.Redis;

using WebApi.Middlewares;
using WebApi.Model;
using WebApi.ReateLimitePolicy;

namespace WebApi.Extensions;

public static class RateLimiterConfigurationExtension
{
    public static IServiceCollection AddRateLimiterConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimiterConfiguration>(configuration.GetSection(RateLimiterConfiguration.Description));

        return services;
    }

    public static IServiceCollection AddRateLimiter(this IServiceCollection services)
    {
        var connectionMultiplexer = services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>();

        services.AddRateLimiter(x => AddRateLimiterOptions(x, connectionMultiplexer));

        return services;
    }

    private static void AddRateLimiterOptions(RateLimiterOptions options, IConnectionMultiplexer connectionMultiplexer)
    {
        options.AddPolicy<string, JwtRateLimiterPolicy>(JwtRateLimiterPolicy.PolicyName);
        options.AddPolicy<IPAddress, ConcurrencyRateLimiterPolicy>(ConcurrencyRateLimiterPolicy.PolicyName);

        options.AddPolicy<string, ClientIdRateLimiterPolicy>(ClientIdRateLimiterPolicy.PolicyName);

        options.AddRedisConcurrencyLimiter("demo_concurrency_queue", (opt) =>
        {
            opt.PermitLimit = 2;
            opt.QueueLimit = 3;
            opt.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.AddRedisConcurrencyLimiter("demo_concurrency", (opt) =>
        {
            opt.PermitLimit = 2;
            opt.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.AddRedisTokenBucketLimiter("demo_token_bucket", (opt) =>
        {
            opt.TokenLimit = 2;
            opt.TokensPerPeriod = 1;
            opt.ReplenishmentPeriod = TimeSpan.FromSeconds(2);
            opt.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.AddRedisFixedWindowLimiter("demo_fixed_window", (opt) =>
        {
            opt.PermitLimit = 1;
            opt.Window = TimeSpan.FromSeconds(2);
            opt.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.AddRedisSlidingWindowLimiter("demo_sliding_window", (options) =>
        {
            options.PermitLimit = 1;                                                 //Quantidade total de registro que será permitido  
            //options.SegmentsPerWindow = 1;                                          //O intervalo do segmento é (tempo da janela)/(segmentos por janela) Valor deve ser maior ou igual a 1
            options.Window = TimeSpan.FromSeconds(2);                                //Tamanho máximo da janela.  
            //options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;        //Caso tenha limite de enfileiramente pego a msg mais velho  
            //options.QueueLimit = 0;                                                 //Não permite msg enfileirada.  Assim que a qts de msg passar de 10 será emitido erro.
            //options.AutoReplenishment = true;                                       //Propriedade responsável por fazer a liberação dos limit que já foram vencidos.  
            options.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.AddRedisConcurrencyLimiter("client2", options =>
        {
            options.PermitLimit = 10;
            options.QueueLimit = 5;
            options.TryDequeuePeriod = TimeSpan.FromSeconds(1);
            options.ConnectionMultiplexerFactory = () => connectionMultiplexer;
        });

        options.OnRejected = (context, ct) => RateLimitMetadata.OnRejected(context.HttpContext, context.Lease, ct);
    }
}
