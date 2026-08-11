using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Redis;
using BuildingBlocks.EventBus.Shared;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EventBus.Extensions;

public static class EventBusExtensions
{
    /// <summary>
    ///     添加统一的 EventBus 支持（支持本地、Redis 与混合分发）
    /// </summary>
    public static IHostApplicationBuilder AddZEventBus(
        this IHostApplicationBuilder builder,
        Action<EventBusOptions>? configure = null)
    {
        var options = builder.Configuration.BindOptions(nameof(EventBusOptions), configure);
        ValidateOptions(options);

        builder.Services.AddConfigurationOptions(nameof(EventBusOptions), configure);
        builder.Services.TryAddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        builder.Services.TryAddSingleton<EventHandlerInvoker>();
        builder.Services.TryAddSingleton(typeof(IEventBus<>), typeof(HybridEventBus<>));

        if (options.EnableLocal)
        {
            builder.Services.TryAddSingleton<EventLocalClient>();
            builder.Services.AddHostedService<EventLocalBackgroundService>();
        }

        if (!options.EnableRedis) return builder;

        builder.Services.TryAddSingleton<RedisEventBusConnection>();
        builder.Services.AddHostedService<RedisEventConsumerService>();

        return builder;
    }

    private static void ValidateOptions(EventBusOptions options)
    {
        if (options.Local.Capacity <= 0)
            throw new InvalidOperationException("EventBusOptions:Local:Capacity 必须大于 0");

        if (!options.EnableRedis) return;

        if (string.IsNullOrWhiteSpace(options.Redis.ConnectionString))
            throw new InvalidOperationException("启用 Redis EventBus 时必须提供 ConnectionString");
        if (options.Redis.IdempotencyExpireSeconds <= 0)
            throw new InvalidOperationException("EventBusOptions:Redis:IdempotencyExpireSeconds 必须大于 0");
        if (options.Redis.ConsumerQueueCapacity <= 0)
            throw new InvalidOperationException("EventBusOptions:Redis:ConsumerQueueCapacity 必须大于 0");
        if (options.Redis.ConsumerConcurrency <= 0)
            throw new InvalidOperationException("EventBusOptions:Redis:ConsumerConcurrency 必须大于 0");
    }
}

/// <summary>
///     内部使用的本地后台消费服务
/// </summary>
internal sealed class EventLocalBackgroundService(EventLocalClient client) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return client.ConsumeStartAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Complete();
        await base.StopAsync(cancellationToken);
    }
}
