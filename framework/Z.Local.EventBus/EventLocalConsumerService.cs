using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Z.Local.EventBus;

/// <summary>
/// 本地事件消费者服务，作为BackgroundService运行
/// </summary>
public class EventLocalConsumerService(ILogger<EventLocalConsumerService> logger,
    EventLocalClient eventLocalClient) : BackgroundService
{
    /// <summary>
    /// 后台服务执行方法
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("事件总线消费者服务已启动");
        await eventLocalClient.ConsumeStartAsync(stoppingToken);
    }

    
}