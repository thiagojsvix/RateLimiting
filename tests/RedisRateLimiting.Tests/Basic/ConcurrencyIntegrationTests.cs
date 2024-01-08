using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using RedisRateLimiting.Tests.Common;

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
namespace RedisRateLimiting.Tests.Basic;

[Collection("Seq")]
public class ConcurrencyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;
    private readonly Request _request;
    private readonly string _apiPath = "/api/v1/concurrency";
    private readonly string _apiPathQueue = "/api/v1/concurrency/queue";

    public ConcurrencyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.CreateClient(options: new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:7255")
        });
        _request = new Request(_httpClient, _apiPath);
    }


    [Fact]
    public async Task GetRequestsEnforceLimit()
    {
        var tasks = new List<Task<RateLimitResponse>>();

        for (var i = 0; i < 5; i++)
        {
            tasks.Add(_request.MakeAsync(_apiPath));
        }

        await Task.WhenAll(tasks);

        tasks.Count(x => x.Result.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(3);
        tasks.Count(x => x.Result.StatusCode == HttpStatusCode.OK).Should().Be(2);

        tasks.Count(x => x.Result.Limit == 2).Should().Be(3);
        tasks.Count(x => x.Result.Remaining == 0).Should().Be(3);

        var response = await _request.MakeAsync(_apiPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Find a way to send rate limit headers when request is successful as well
        response.Limit.Should().BeNull();
        response.Remaining.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQueuedRequests()
    {
        var tasks = new List<Task<RateLimitResponse>>();

        for (var i = 0; i < 5; i++)
        {
            tasks.Add(_request.MakeAsync(_apiPathQueue));
        }

        await Task.WhenAll(tasks);

        tasks.Count(x => x.Result.StatusCode == HttpStatusCode.OK).Should().Be(5);
    }
}
