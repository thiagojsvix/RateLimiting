using WebApi.Database;
using WebApi.Extensions;
using WebApi.Settings;

var builder = WebApplication.CreateBuilder(args);

var serviceSettings = builder.Configuration.GetSection("ServiceSettings").Get<ServiceSettings>();

//Adiciona contexto do banco de dados ao mecanismo de DI 
builder.Services.AddDbContext<RateLimiteDbContext>();

builder.Services.AddRedis();

builder.Services
    .AddSecurity()
    .AddRateLimiterConfig(builder.Configuration)
    .AddRateLimiter()
    .AddConsulSettings(serviceSettings)
    ;

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConsul(serviceSettings);

//app.UseHttpsRedirection();

app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();


using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<RateLimiteDbContext>();
dbContext.Database.EnsureCreated();

//definindo porta que a aplicação ira escutar
app.Run($"http://*:{serviceSettings.ServicePort}");

// Hack: make the implicit Program class public so test projects can access it
public partial class Program { }
