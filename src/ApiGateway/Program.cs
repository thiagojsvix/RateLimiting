using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration.AddJsonFile("ocelot.json", false, true)
                                          .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", true, true)
                                          ;

builder.Services.AddControllers();


builder.Services.AddOcelot(configuration.Build())
                .AddConsul();

var app = builder.Build();


app.UseOcelot().Wait();

app.Run("http://*:9000");
