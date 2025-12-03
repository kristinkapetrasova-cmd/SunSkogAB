using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SunSkog.Api.Tests;

public class SwaggerSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Swagger_Index_Should_Return_200()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", html);
    }
}