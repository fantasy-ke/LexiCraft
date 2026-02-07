namespace BuildingBlocks.MassTransit.Options;

public class MassTransitOptions
{
    public const string SectionName = "MassTransit";

    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;
    
    /// <summary>
    /// 服务名称，用于队列命名或端点命名的前缀
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 消息消费的重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 重试间隔（秒）
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;
}
