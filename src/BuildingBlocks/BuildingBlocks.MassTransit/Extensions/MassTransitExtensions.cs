using System.Reflection;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.MassTransit.EventSourcing.Services;
using BuildingBlocks.MassTransit.EventSourcing.Store;
using BuildingBlocks.MassTransit.Options;
using BuildingBlocks.MassTransit.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.MassTransit.Extensions;

public static class MassTransitExtensions
{
    /// <summary>
    /// 添加 MassTransit 服务，支持 RabbitMQ 和灵活配置。
    /// 同时支持 Saga 持久化 (MongoDB) 和简单的事件溯源 (Redis)。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="assemblies">需要扫描 Consumer 的程序集</param>
    /// <param name="configure">额外的 MassTransit 配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCustomMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[]? assemblies = null,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var options = new MassTransitOptions();
        configuration.GetSection(MassTransitOptions.SectionName).Bind(options);
        services.Configure<MassTransitOptions>(configuration.GetSection(MassTransitOptions.SectionName));

        // 注册统一事件发布者
        services.TryAddScoped<IEventPublisher, EventPublisher>();

        // 独立方法引入事件溯源服务
        services.AddEventSourcing(options);

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            if (assemblies != null && assemblies.Length > 0)
            {
                x.AddConsumers(assemblies);
                x.AddSagaStateMachines(assemblies);
                x.AddSagas(assemblies);
                x.AddActivities(assemblies);
            }

            // 独立方法配置 Saga 存储
            x.ConfigureSagaRepository(options);

            // 允许自定义配置 (例如 Sagas, Activities)
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, (ushort)options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // 全局预取计数
                cfg.PrefetchCount = options.PrefetchCount;

                // 并发限制
                if (options.ConcurrencyLimit.HasValue)
                {
                    cfg.UseConcurrencyLimit(options.ConcurrencyLimit.Value);
                }

                // 配置所有已注册 Consumer 的端点
                cfg.ConfigureEndpoints(context);

                // 默认重试策略
                cfg.UseMessageRetry(r => r.Interval(options.RetryCount, TimeSpan.FromSeconds(options.RetryIntervalSeconds)));

                // 断路器策略
                if (options.UseCircuitBreaker)
                {
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = options.CircuitBreakerTripThreshold;
                        cb.ActiveThreshold = options.CircuitBreakerActiveThreshold;
                        cb.ResetInterval = TimeSpan.FromSeconds(options.CircuitBreakerResetIntervalSeconds);
                    });
                }
            });
        });

        return services;
    }

    /// <summary>
    /// 注册事件溯源相关服务 (使用 Redis 存储)
    /// </summary>
    private static IServiceCollection AddEventSourcing(this IServiceCollection services, MassTransitOptions options)
    {
        if (!options.EventSourcing.Enabled)
        {
            return services;
        }

        // 注册 Redis 连接 (单例)
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var currentOptions = sp.GetRequiredService<IOptions<MassTransitOptions>>().Value;
            var eventSourcingOptions = currentOptions.EventSourcing;

            // 确定使用的 Redis 连接字符串
            var connectionString = eventSourcingOptions.RedisConnectionString;

            var config = ConfigurationOptions.Parse(connectionString);
            return ConnectionMultiplexer.Connect(config);
        });

        // 注册 RedisEventStore
        services.TryAddSingleton<IEventStore, RedisEventStore>();

        services.TryAddScoped<IEventReplayer, EventReplayer>();

        return services;
    }

    /// <summary>
    /// 配置 Saga 存储库 (目前支持 MongoDb)
    /// </summary>
    private static void ConfigureSagaRepository(this IBusRegistrationConfigurator x, MassTransitOptions options)
    {
        if (!options.Saga.Enabled)
        {
            return;
        }

        // 仅支持 MongoDb，Redis 支持已移除
        if (options.Saga.RepositoryType == SagaRepositoryType.MongoDb)
        {
            x.SetMongoDbSagaRepositoryProvider(cfg =>
            {
                cfg.Connection = options.Saga.MongoDb.ConnectionString;
                cfg.DatabaseName = options.Saga.MongoDb.DatabaseName;
                if (!string.IsNullOrWhiteSpace(options.Saga.MongoDb.CollectionName))
                {
                    cfg.CollectionName = options.Saga.MongoDb.CollectionName;
                }
            });
        }
    }
}
