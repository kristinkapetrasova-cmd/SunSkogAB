// File: SunSkog.Api.Tests/HealthSmokeTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using SunSkog.Api;

public class HealthSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Returns_200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Ping_Returns_Pong()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/ping");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<PingDto>();
        Assert.NotNull(body);
        Assert.Equal("pong", body!.message);
    }

    private record PingDto(string message);
}