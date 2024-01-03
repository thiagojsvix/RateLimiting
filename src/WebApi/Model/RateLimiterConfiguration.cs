namespace WebApi.Model;

public class RateLimiterConfiguration
{
    public const string Description = "RateLimiterConfiguration";

    /// <summary>
    /// Número máximo de contadores de permissão que podem ser permitidos em uma janela. 
    /// Deve ser definido como um valor > 0 no momento em que essas opções são passadas 
    /// para o construtor de FixedWindowRateLimiter.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Especifica a janela de tempo que recebe as solicitações. 
    /// Deve ser definido como um valor maior do que Zero quando 
    /// essas opções são passadas para o construtor de FixedWindowRateLimiter.
    /// </summary>
    public int Window { get; set; } 

    /// <summary>
    /// Especifica se o FixedWindowRateLimiter é automaticamente atualizar contadores 
    /// ou se outra pessoa chamará TryReplenish() para atualizar contadores.
    /// </summary>
    public int ReplenishmentPeriod { get; set; } 

    /// <summary>
    /// Contagem máxima de permissões cumulativas de solicitações de aquisição enfileiradas. 
    /// Deve ser definido como um valor >= 0 no momento em que essas opções são passadas para o construtor de FixedWindowRateLimiter.
    /// </summary>
    public int QueueLimit { get; set; } 

    /// <summary>
    /// specifica o número máximo de segmentos em que uma janela é dividida. 
    /// Deve ser definido como um valor > 0 no momento em que essas opções são passadas para o construtor de SlidingWindowRateLimiter.
    /// </summary>
    public int SegmentsPerWindow { get; set; } 

    /// <summary>
    /// Número máximo de tokens que podem estar no bucket a qualquer momento. 
    /// Deve ser definido como um valor > 0 no momento em que essas opções são passadas para o construtor de TokenBucketRateLimiter.
    /// </summary>
    public int TokenLimit { get; set; }

    /// <summary>
    /// Especifica o número máximo de tokens para restaurar cada reabastecimento. 
    /// Deve ser definido como um valor > 0 no momento em que essas opções são passadas para o construtor de TokenBucketRateLimiter.
    /// </summary>
    public int TokensPerPeriod { get; set; } 

    /// <summary>
    /// Especificado se o TokenBucketRateLimiter está reabastecendo tokens automaticamente ou se outra pessoa chamará TryReplenish() para repor tokens.
    /// </summary>
    public bool AutoReplenishment { get; set; }
}
