using BuildingBlocks.Exceptions.Problem;
using LexiCraft.Services.Practice.Shared.Exceptions;
using LexiCraft.Services.Practice.Shared.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;

public static class ExceptionHandlingExtensions
{
    public static WebApplicationBuilder AddPracticeExceptionHandling(this WebApplicationBuilder builder)
    {
        // Register the custom problem code mapper for Practice service
        builder.Services.AddSingleton<IProblemCodeMapper, PracticeProblemCodeMapper>();
        
        return builder;
    }

    public static WebApplicationBuilder AddDatabaseResilience(this WebApplicationBuilder builder)
    {
        // MongoDB resilience and performance monitoring are now automatically registered
        // by BuildingBlocks.MongoDB.Extensions.DependencyInjectionExtensions.AddMongoDbContext
        
        // Add MongoDB health check
        builder.Services.AddHealthChecks()
            .AddCheck<MongoDbHealthCheck>("mongodb", tags: new[] { "database", "mongodb" });
        
        return builder;
    }
}