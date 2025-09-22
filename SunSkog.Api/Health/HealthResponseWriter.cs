// SunSkog.Api/Health/HealthResponseWriter.cs
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SunSkog.Api.Health;

public static class HealthResponseWriter
{
    public static Task WriteJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            results = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    error = kvp.Value.Exception?.Message,
                    durationMs = kvp.Value.Duration.TotalMilliseconds
                })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return context.Response.WriteAsync(json);
    }
}