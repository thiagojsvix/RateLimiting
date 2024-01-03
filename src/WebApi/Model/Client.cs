namespace WebApi.Model;

public class Client
{
    public long Id { get; set; }

    public string Identifier { get; set; }

    public long RateLimitId { get; set; }

    public RateLimit RateLimit { get; set; }
}
