using System.Reflection;
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
    /// 添加 MassTransit 服务，支持 RabbitMQ 和灵活配置。
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

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            if (assemblies != null && assemblies.Length > 0)
            {
                x.AddConsumers(assemblies);
            }

            // 允许自定义配置 (例如 Sagas, Activities)
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, (ushort)options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // 配置所有已注册 Consumer 的端点
                cfg.ConfigureEndpoints(context);

                // 默认重试策略
                cfg.UseMessageRetry(r => r.Interval(options.RetryCount, TimeSpan.FromSeconds(options.RetryIntervalSeconds)));
            });
        });

        return services;
    }
}
