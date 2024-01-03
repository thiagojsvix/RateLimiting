using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using RedisRateLimiting.Tests.Common;

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
namespace RedisRateLimiting.Tests;

[Collection("Seq")]
public class ClientIdPolicyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;
    private readonly Request _request;
    private readonly string _apiPath = "clients";

    public ClientIdPolicyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.CreateClient(options: new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:7255"),
        });
        
        _request = new Request(_httpClient, _apiPath);
    }

    [Theory]
    [InlineData("client1", 1)]
    [InlineData("client2", 2)]
    [InlineData("client3", 3)]
    [InlineData("clientX", 1)]
    public async Task GetRequestsEnforceLimit(string clientId, int permitLimit)
    {
        var tasks = new List<Task<RateLimitResponse>>();
        var extra = 2;
        
        _request.AddHeader("X-ClientId", clientId);

        for (var i = 0; i < permitLimit + extra; i++)
        {
            tasks.Add(_request.MakeAsync());
        }

        await Task.WhenAll(tasks);

        tasks.Count(x => x.Result.StatusCode == HttpStatusCode.OK).Should().Be(permitLimit);
        tasks.Count(x => x.Result.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(extra);
    }
}
