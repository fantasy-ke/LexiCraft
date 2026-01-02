using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Redis;
using BuildingBlocks.EventBus.Shared;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EventBus.Extensions;

public static class EventBusExtensions
{
    /// <summary>
    /// 添加统一的 EventBus 支持 (支持混合模式与配置文件绑定)
    /// </summary>
    public static IHostApplicationBuilder AddZEventBus(this IHostApplicationBuilder builder, Action<EventBusOptions>? configure = null)
    {
        // 直接从 builder.Configuration 获取配置实例以便进行条件注册
        var options = builder.Configuration.BindOptions(nameof(EventBusOptions), configure);

        // 依然注册 Options 以供后续运行时注入使用 (IOptionsSnapshot etc.)
        builder.Services.AddConfigurationOptions(nameof(EventBusOptions), configure);

        builder.Services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        builder.Services.AddSingleton(typeof(IEventBus<>), typeof(HybridEventBus<>));

        // 如果启用本地总线
        if (options.EnableLocal)
        {
            builder.Services.AddSingleton<EventLocalClient>();
            builder.Services.AddHostedService<EventLocalBackgroundService>();
        }

        // 如果启用 Redis 总线
        if (!options.EnableRedis) return builder;
        if (string.IsNullOrEmpty(options.Redis.ConnectionString))
        {
            throw new Exception("启用 Redis EventBus 时必须提供 ConnectionString");
        }

        builder.Services.AddSingleton(new FreeRedis.RedisClient(options.Redis.ConnectionString));
        builder.Services.AddHostedService<RedisEventConsumerService>();

        return builder;
    }
}

/// <summary>
/// 内部使用的本地后台消费服务
/// </summary>
internal class EventLocalBackgroundService(EventLocalClient client) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => client.ConsumeStartAsync(stoppingToken);
}
