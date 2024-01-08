using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using StackExchange.Redis;

namespace WebApi.Extensions;

public static class RedisExtension
{
    private static readonly EndPointCollection endPoints = new() { { "localhost", 6379 } };

    private static readonly ConfigurationOptions options = new()
    {
        EndPoints = endPoints,                                               // Lista de Endereço dos servidores redis
        User = "default",                                                    // Use seu usuário redis. Para mais informações consulte o site https://redis.io/docs/management/security/acl/
        Password = "eYVX7EwVmmxKPCDmwMtyKVge8oLd2t81",                       // use your Redis password
        Ssl = false,                                                         // somente habilitar o ssl se for configurado no container do redis   
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12,    // define que iremos utilizar o protoco TLS com o certificado
        AbortOnConnectFail = false,                                          // quando a conexão fica muito tempo ociosa ela é fechado e essa propriedade evita que gere uma exceção. 
                                                                             // quando necessário usar o redis novamente automaticamente a conexão será reestabelecida.

    };

    private static X509Certificate2 GetCertificateFromThubprint()
    {
        // Find certificate from "certificate store" based on thumbprint and return
        StoreName CertStoreName = StoreName.My;
        string PFXThumbPrint = "e7c783f97ccce40d9390faa72b18333da02a7951";
        X509Store certLocalMachineStore = new X509Store(CertStoreName, StoreLocation.CurrentUser);
        certLocalMachineStore.Open(OpenFlags.ReadOnly);
        X509Certificate2Collection certLocalMachineCollection = certLocalMachineStore.Certificates.Find(
                                   X509FindType.FindByThumbprint, PFXThumbPrint, true);

        certLocalMachineStore.Close();

        return certLocalMachineCollection[0];
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate == null)
            return false;

        var ca = new X509Certificate2("redis_ca.pem");
        bool verdict = (certificate.Issuer == ca.Subject);

        if (verdict)
            return true;

        Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

        return false;
    }

    private static X509Certificate2 Load_CertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
    {
        var certificate = GetCertificateFromThubprint();

        if (certificate == null)
        {
            return new X509Certificate2("redis.pfx", "redis-certificado-senha"); // use the password you specified for pfx file
        }

        return certificate;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services)
    {
        options.CertificateSelection += Load_CertificateSelection;
        options.CertificateValidation += ValidateServerCertificate;

        var connectionMultiplexer = ConnectionMultiplexer.Connect(options);

        services.AddSingleton<IConnectionMultiplexer>(sp => connectionMultiplexer);

        return services;
    }
}
