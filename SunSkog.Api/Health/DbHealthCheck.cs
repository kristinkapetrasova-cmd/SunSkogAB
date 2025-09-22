// SunSkog.Api/Health/DbHealthCheck.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SunSkog.Api.Data;

namespace SunSkog.Api.Health;

public sealed class DbHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DbHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var can = await _db.Database.CanConnectAsync(cancellationToken);
            return can
                ? HealthCheckResult.Healthy("DB reachable")
                : HealthCheckResult.Unhealthy("DB not reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DB check failed", ex);
        }
    }
}