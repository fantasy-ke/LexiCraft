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

    /// <summary>
    /// 预取计数 (PrefetchCount)，控制消费者一次从队列获取的消息数量
    /// 默认值：16
    /// </summary>
    public int PrefetchCount { get; set; } = 16;

    /// <summary>
    /// 并发限制 (ConcurrencyLimit)，控制每个消费者实例并行处理消息的最大数量
    /// 如果未设置，通常由 PrefetchCount 决定
    /// </summary>
    public int? ConcurrencyLimit { get; set; }

    /// <summary>
    /// 是否启用断路器 (Circuit Breaker)
    /// </summary>
    public bool UseCircuitBreaker { get; set; } = false;

    /// <summary>
    /// 断路器触发阈值 (百分比 0-100)，默认 15%
    /// </summary>
    public int CircuitBreakerTripThreshold { get; set; } = 15;

    /// <summary>
    /// 断路器活跃请求数阈值，只有当活跃请求数达到此值时才计算失败率，默认 10
    /// </summary>
    public int CircuitBreakerActiveThreshold { get; set; } = 10;

    /// <summary>
    /// 断路器重置间隔（秒），默认 60 秒
    /// </summary>
    public int CircuitBreakerResetIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Saga 持久化配置
    /// </summary>
    public SagaOptions Saga { get; set; } = new();

    /// <summary>
    /// 事件溯源配置
    /// </summary>
    public EventSourcingOptions EventSourcing { get; set; } = new();
}

/// <summary>
/// Saga 配置选项
/// </summary>
public class SagaOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Saga 存储类型: 默认 MongoDb
    /// </summary>
    public SagaRepositoryType RepositoryType { get; set; } = SagaRepositoryType.MongoDb;

    public MongoDbSagaOptions MongoDb { get; set; } = new();
}

public class MongoDbSagaOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "sagas";
    public string? CollectionName { get; set; }
}

/// <summary>
/// 事件溯源配置选项
/// </summary>
public class EventSourcingOptions
{
    /// <summary>
    /// 是否启用事件溯源
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 事件存储的 Redis 连接字符串
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Redis Stream Key 前缀
    /// </summary>
    public string StreamPrefix { get; set; } = "events:";
}
