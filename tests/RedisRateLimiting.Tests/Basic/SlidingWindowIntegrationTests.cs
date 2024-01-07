using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using RedisRateLimiting.Tests.Common;

namespace RedisRateLimiting.Tests.Basic;

[Collection("Seq")]
public partial class SlidingWindowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Request _request;
    private readonly HttpClient _httpClient;
    private readonly string _apiPath = "/SlidingWindow";

    public SlidingWindowIntegrationTests(WebApplicationFactory<Program> factory)
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
        var response = await _request.MakeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        //response.Limit.Should().Be(1);
        //response.Remaining.Should().Be(0);

        var plusDelay = (response.RetryAfter ?? 0) + 2;
        await Task.Delay(1000 * plusDelay);

        response = await _request.MakeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
