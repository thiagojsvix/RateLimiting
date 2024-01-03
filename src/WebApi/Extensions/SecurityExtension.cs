namespace WebApi.Extensions;

public static class SecurityExtension
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddAuthentication("Bearer").AddJwtBearer();

        return services;
    }
}
