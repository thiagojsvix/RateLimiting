namespace WebApi.Model;

public class RateLimit
{
    public long Id { get; set; }

    public int PermitLimit { get; set; }

    public int QueueLimit { get; set; }

    public TimeSpan Window { get; set; }

    public List<Client> Clients { get; set; }
}
