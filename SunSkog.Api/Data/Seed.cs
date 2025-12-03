using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SunSkog.Api.Data;

public static class Seed
{
    public static async Task AppDataAsync(IServiceProvider services, ILogger logger)
    {
        // Sem případně doplníme seed ne-identitových dat (projekty, sazby, atd.)
        await Task.CompletedTask;
    }
}
