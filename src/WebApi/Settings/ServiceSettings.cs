namespace WebApi.Settings;

public class ServiceSettings
{
    public string ServiceName { get; set; }
    public string ServiceHost { get; set; }
    public int ServicePort { get; set; }
    public string ServiceDiscoveryAddress { get; set; }
    public int GatewayPort { get; set;}

    /// <summary>
    /// Essa propriedade só deve ser definida em modo desenvolvimento quando o Consul estiver rodando no Docker e a API estiver rodando localmente
    /// Quando o Consult estiver no Container e a API local, existe um alias do docker que permite que o container acesse o host a url é: host.docker.internal
    /// Essa propriedade só está disponivel apartir da versão Docker Desktop 18.03+
    /// </summary>
    public string GatewayHost { get; set; }

    public string RedisHost { get; set; }
}
