using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using WebApi.Model;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("client2")]
public class Clients2Controller : ControllerBase
{
    private static readonly string[] Summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await Task.Delay(0000);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
