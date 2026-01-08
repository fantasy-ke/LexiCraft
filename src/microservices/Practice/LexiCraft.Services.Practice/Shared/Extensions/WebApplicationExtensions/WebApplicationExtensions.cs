using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public static IEndpointRouteBuilder UseInfrastructure(this IEndpointRouteBuilder app)
    {
        // Initialize database and run seeding if needed
        using var scope = app.ServiceProvider.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<PracticeDbDataSeeder>();
        
        // Run seeding asynchronously (fire and forget for startup performance)
        _ = Task.Run(async () =>
        {
            try
            {
                await dataSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail startup
                var logger = scope.ServiceProvider.GetService<ILogger<PracticeDbDataSeeder>>();
                logger?.LogError(ex, "Failed to seed Practice database");
            }
        });

        return app;
    }
}