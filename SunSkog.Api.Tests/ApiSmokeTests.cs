using SunSkog.Api;                              // kvůli typu Program z web projektu
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace SunSkog.Api.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Swagger_Is_Reachable()
    {
        var resp = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}