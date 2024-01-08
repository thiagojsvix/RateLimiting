using Consul;

using WebApi.Settings;

namespace WebApi.Extensions;

public static class ConsulExtension
{
    public static IServiceCollection AddConsulSettings(this IServiceCollection services, ServiceSettings serviceSettings)
    {
        services.AddTransient<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
        {
            consulConfig.Address = new Uri(serviceSettings.ServiceDiscoveryAddress);
        }));

        return services;
    }

    public static IApplicationBuilder UseConsul(this IApplicationBuilder app, ServiceSettings serviceSettings)
    {
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("AppExtensions");
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        /*
         * Essa configura em ambiente de desenvolvimento é retirada do arquivo appsettings.json
         * Quando estiver rodando no docker vai pegar as variáveis de ambiente definida no arquivo
         * docker-compose.yml
         *
         * Quando o Consul estiver sendo executado no Docker e a API fora é necessário usar o alias host.docker.internal 
         * para que o consult dentro do container consiga acessar o endereço do endpoint de health check.
         *
         * A porta deve ser a porta do Gateway, ou seja, pegar a porta que foi configurado para o Ocelot escutar
         * No caso atual o Ocelot está configurado para ouvir a porta 9000.
         * 
         * Quando a API e o consul estiver sendo executada dentro do container pode ser informado direto o nome do 
         * servidor do consul, nesse caso a propriedade ServiceSettings.ServiceHost
         */

        var healthCheckHttp = $"http://{serviceSettings.GatewayHost}:{serviceSettings.GatewayPort}/healthcheck/status";

        var agentCheckApi = new AgentCheckRegistration()
        {
            HTTP = healthCheckHttp,
            Notes = $"Checks {healthCheckHttp}",
            Timeout = TimeSpan.FromSeconds(30),
            Interval = TimeSpan.FromSeconds(10),
            Status = HealthStatus.Passing,
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
        };

        var agentCheckTcp = new AgentCheckRegistration()
        {
            TCP = $"{serviceSettings.GatewayHost}:{serviceSettings.GatewayPort}",
            Notes = $"Runs a TCP check on Host {serviceSettings.GatewayHost} and port {serviceSettings.GatewayPort}",
            Timeout = TimeSpan.FromSeconds(10),
            Interval = TimeSpan.FromSeconds(30),
            Status = HealthStatus.Passing,
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(50)
        };

        var registration = new AgentServiceRegistration()
        {
            //Id server para identificar o host dentro do Consul
            //quando trabalha com varios container do docker isso
            //é útil para saber o nome do host e quando ele foi 
            //inscrito no Consul
            ID = serviceSettings.ServiceHost + $"-{DateTime.UtcNow.AddHours(-3):HH:mm:ss.FFFF}",
            Name = serviceSettings.ServiceName,
            Address = serviceSettings.ServiceHost,
            Port = serviceSettings.ServicePort,
            Checks = [agentCheckApi, agentCheckTcp],
        };

        var log = $"""
                   Registering with Consul:
                        ServiceId...: {registration.ID},
                        ServiceName.: {registration.Name},
                        ServiceHost.: {registration.Address},
                        ServicePort.: {registration.Port}
                   """;

        logger.LogInformation(log);

        consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
        consulClient.Agent.ServiceRegister(registration).ConfigureAwait(true);

        //https://cecilphillip.com/using-consul-for-health-checks-with-asp-net-core/
        var services = consulClient.Agent.Services().Result.Response;
        foreach (var service in services)
        {
            var checks = consulClient.Health.Checks(serviceSettings.ServiceName).Result;

            foreach (var checkResult in checks.Response)
            {
                if (checkResult.Status.Status == "critical")
                    logger.LogError($" Verificando status: {checkResult.ServiceID} - {checkResult.Status.Status}");
                else
                    logger.LogInformation($" Verificando status: {checkResult.ServiceID} - {checkResult.Status.Status}");
            }
        }

        lifetime.ApplicationStopping.Register(() =>
        {
            logger.LogInformation("Unregistering from Consul");
        });


        return app;
    }
}
