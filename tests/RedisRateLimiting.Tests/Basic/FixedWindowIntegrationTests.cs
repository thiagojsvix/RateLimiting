using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using RedisRateLimiting.Tests.Common;

namespace RedisRateLimiting.Tests.Basic;

[Collection("Seq")]
public class FixedWindowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Request _request;
    private readonly HttpClient _httpClient;
    private readonly string _apiPath = "/api/v1/fixedwindow";

    public FixedWindowIntegrationTests(WebApplicationFactory<Program> factory)
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await _request.MakeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        response.Limit.Should().Be(1);
        response.Remaining.Should().Be(0);
        response.RetryAfter.Should().NotBeNull();

        var plusDelay = (response.RetryAfter ?? 0) + 1;
        await Task.Delay(1000 * plusDelay);

        response = await _request.MakeAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
