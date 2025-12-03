using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SunSkog.Api.Data;

public class DesignTimeApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(cs)
            .Options;

        return new ApplicationDbContext(opts);
    }
}