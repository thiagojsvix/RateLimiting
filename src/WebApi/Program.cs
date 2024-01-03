using WebApi.Database;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

//Adiciona contexto do banco de dados ao mecanismo de DI 
builder.Services.AddDbContext<RateLimiteDbContext>();

builder.Services.AddRedis();

builder.Services
    .AddSecurity()
    .AddRateLimiterConfig(builder.Configuration)
    .AddRateLimiter()
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

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();


using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<RateLimiteDbContext>();
dbContext.Database.EnsureCreated();


app.Run();

// Hack: make the implicit Program class public so test projects can access it
public partial class Program { }
