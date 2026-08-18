using System.Reflection;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.MassTransit.EventSourcing.Services;
using BuildingBlocks.MassTransit.EventSourcing.Store;
using BuildingBlocks.MassTransit.LocalEvents;
using BuildingBlocks.MassTransit.Options;
using BuildingBlocks.MassTransit.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.MassTransit.Extensions;

public static class MassTransitExtensions
{
    /// <summary>
    ///     添加 MassTransit、可选 Saga、可选事件溯源和有界本地事件队列。
    /// </summary>
    public static IServiceCollection AddCustomMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[]? assemblies = null,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var section = configuration.GetSection(MassTransitOptions.SectionName);
        var options = new MassTransitOptions();
        section.Bind(options);
        services.Configure<MassTransitOptions>(section);

        if (!options.Enabled) return services;
        ValidateOptions(options);

        services.TryAddScoped<IEventPublisher, EventPublisher>();
        services.TryAddSingleton<ILocalEventBus, LocalEventBus>();
        services.AddHostedService<LocalEventBackgroundService>();
        services.AddEventSourcing(options, assemblies);

        services.AddMassTransit(registration =>
        {
            registration.SetKebabCaseEndpointNameFormatter();

            if (assemblies is { Length: > 0 })
            {
                registration.AddConsumers(assemblies);
                registration.AddSagaStateMachines(assemblies);
                registration.AddSagas(assemblies);
                registration.AddActivities(assemblies);
            }

            registration.ConfigureSagaRepository(options);
            configure?.Invoke(registration);

            registration.UsingRabbitMq((context, rabbitMq) =>
            {
                rabbitMq.Host(options.Host, (ushort)options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                rabbitMq.PrefetchCount = options.PrefetchCount;

                if (options.ConcurrencyLimit.HasValue)
                    rabbitMq.UseConcurrencyLimit(options.ConcurrencyLimit.Value);

                if (options.RetryCount > 0)
                    rabbitMq.UseMessageRetry(retry =>
                        retry.Interval(options.RetryCount, TimeSpan.FromSeconds(options.RetryIntervalSeconds)));

                if (options.UseCircuitBreaker)
                    rabbitMq.UseCircuitBreaker(circuitBreaker =>
                    {
                        circuitBreaker.TrackingPeriod = TimeSpan.FromMinutes(1);
                        circuitBreaker.TripThreshold = options.CircuitBreakerTripThreshold;
                        circuitBreaker.ActiveThreshold = options.CircuitBreakerActiveThreshold;
                        circuitBreaker.ResetInterval =
                            TimeSpan.FromSeconds(options.CircuitBreakerResetIntervalSeconds);
                    });

                rabbitMq.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection AddEventSourcing(
        this IServiceCollection services,
        MassTransitOptions options,
        Assembly[]? eventAssemblies)
    {
        if (!options.EventSourcing.Enabled) return services;

        services.TryAddSingleton<EventStoreRedisConnection>();
        services.TryAddSingleton<IEventTypeResolver>(
            new EventTypeResolver(eventAssemblies ?? []));
        services.TryAddSingleton<IEventStore, RedisEventStore>();
        services.TryAddScoped<IEventReplayer, EventReplayer>();
        services.TryAddScoped<IDomainEventReplayer, DomainEventReplayer>();

        return services;
    }

    private static void ConfigureSagaRepository(
        this IBusRegistrationConfigurator registration,
        MassTransitOptions options)
    {
        if (!options.Saga.Enabled) return;

        if (options.Saga.RepositoryType != SagaRepositoryType.MongoDb)
            throw new NotSupportedException(
                $"当前仅支持 MongoDb Saga Repository，实际配置: {options.Saga.RepositoryType}");

        registration.SetMongoDbSagaRepositoryProvider(repository =>
        {
            repository.Connection = options.Saga.MongoDb.ConnectionString;
            repository.DatabaseName = options.Saga.MongoDb.DatabaseName;
            if (!string.IsNullOrWhiteSpace(options.Saga.MongoDb.CollectionName))
                repository.CollectionName = options.Saga.MongoDb.CollectionName;
        });
    }

    private static void ValidateOptions(MassTransitOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
            throw new InvalidOperationException("MassTransit:Host 不能为空");
        if (options.Port is <= 0 or > ushort.MaxValue)
            throw new InvalidOperationException("MassTransit:Port 必须介于 1 和 65535 之间");
        if (string.IsNullOrWhiteSpace(options.VirtualHost))
            throw new InvalidOperationException("MassTransit:VirtualHost 不能为空");
        if (options.PrefetchCount <= 0)
            throw new InvalidOperationException("MassTransit:PrefetchCount 必须大于 0");
        if (options.ConcurrencyLimit <= 0)
            throw new InvalidOperationException("MassTransit:ConcurrencyLimit 必须大于 0");
        if (options.RetryCount < 0)
            throw new InvalidOperationException("MassTransit:RetryCount 不能小于 0");
        if (options.RetryIntervalSeconds < 0)
            throw new InvalidOperationException("MassTransit:RetryIntervalSeconds 不能小于 0");
        if (options.LocalEvents.Capacity <= 0)
            throw new InvalidOperationException("MassTransit:LocalEvents:Capacity 必须大于 0");

        if (options.UseCircuitBreaker)
        {
            if (options.CircuitBreakerTripThreshold is <= 0 or > 100)
                throw new InvalidOperationException("MassTransit:CircuitBreakerTripThreshold 必须介于 1 和 100 之间");
            if (options.CircuitBreakerActiveThreshold <= 0)
                throw new InvalidOperationException("MassTransit:CircuitBreakerActiveThreshold 必须大于 0");
            if (options.CircuitBreakerResetIntervalSeconds <= 0)
                throw new InvalidOperationException("MassTransit:CircuitBreakerResetIntervalSeconds 必须大于 0");
        }

        if (options.EventSourcing.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.EventSourcing.RedisConnectionString))
                throw new InvalidOperationException("MassTransit:EventSourcing:RedisConnectionString 不能为空");
            if (string.IsNullOrWhiteSpace(options.EventSourcing.StreamPrefix))
                throw new InvalidOperationException("MassTransit:EventSourcing:StreamPrefix 不能为空");
            if (options.EventSourcing.ReadBatchSize <= 0)
                throw new InvalidOperationException("MassTransit:EventSourcing:ReadBatchSize 必须大于 0");
        }

        if (!options.Saga.Enabled) return;

        if (options.Saga.RepositoryType != SagaRepositoryType.MongoDb)
            throw new NotSupportedException(
                $"当前仅支持 MongoDb Saga Repository，实际配置: {options.Saga.RepositoryType}");
        if (string.IsNullOrWhiteSpace(options.Saga.MongoDb.ConnectionString))
            throw new InvalidOperationException("MassTransit:Saga:MongoDb:ConnectionString 不能为空");
        if (string.IsNullOrWhiteSpace(options.Saga.MongoDb.DatabaseName))
            throw new InvalidOperationException("MassTransit:Saga:MongoDb:DatabaseName 不能为空");
    }
}
