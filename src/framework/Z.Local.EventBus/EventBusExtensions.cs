using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Z.EventBus;
using Z.Local.EventBus.Serializer;

namespace Z.Local.EventBus;

/// <summary>
/// 事件总线扩展方法
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// 添加事件总线消费者服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLocalEventBus(this IServiceCollection services)
    {
        services
            .AddSingleton(typeof(IEventBus<>), typeof(LocalEventBus<>));
        
        services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        
        services.AddSingleton<EventLocalClient>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<EventLocalClient>>();
            var handlerSerializer = sp.GetRequiredService<IHandlerSerializer>();
            return ActivatorUtilities.CreateInstance<EventLocalClient>(sp, log, handlerSerializer);
        });
        // 注册事件消费者服务作为托管服务
        services.AddHostedService<EventLocalConsumerService>();
        
        return services;
    }
}