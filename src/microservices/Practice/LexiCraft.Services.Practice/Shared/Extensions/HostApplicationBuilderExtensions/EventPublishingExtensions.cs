using BuildingBlocks.EventBus.Extensions;
using LexiCraft.Services.Practice.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds event publishing services with resilience patterns
    /// </summary>
    public static IHostApplicationBuilder AddEventPublishing(this IHostApplicationBuilder builder)
    {
        // Add EventBus support
        builder.AddZEventBus();
        
        // Register event publishing services
        builder.Services.AddScoped<IPracticeEventPublisher, PracticeEventPublisher>();
        builder.Services.AddScoped<IPerformanceDataService, PerformanceDataService>();
        
        return builder;
    }
}