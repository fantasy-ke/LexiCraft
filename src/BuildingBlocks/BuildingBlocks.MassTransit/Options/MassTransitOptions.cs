namespace BuildingBlocks.MassTransit.Options;

public class MassTransitOptions
{
    public const string SectionName = "MassTransit";

    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;

    /// <summary>
    ///     服务名称，保留给业务侧区分服务实例或自定义端点命名。
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     消息消费的重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    ///     重试间隔（秒）
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>
    ///     预取计数，控制消费者一次从队列获取的消息数量
    /// </summary>
    public int PrefetchCount { get; set; } = 16;

    /// <summary>
    ///     每个消费者实例并行处理消息的最大数量
    /// </summary>
    public int? ConcurrencyLimit { get; set; }

    /// <summary>
    ///     是否启用断路器
    /// </summary>
    public bool UseCircuitBreaker { get; set; }

    /// <summary>
    ///     断路器触发阈值（百分比 0-100）
    /// </summary>
    public int CircuitBreakerTripThreshold { get; set; } = 15;

    /// <summary>
    ///     断路器活跃请求数阈值
    /// </summary>
    public int CircuitBreakerActiveThreshold { get; set; } = 10;

    /// <summary>
    ///     断路器重置间隔（秒）
    /// </summary>
    public int CircuitBreakerResetIntervalSeconds { get; set; } = 60;

    /// <summary>
    ///     本地事件队列配置
    /// </summary>
    public LocalEventOptions LocalEvents { get; set; } = new();

    /// <summary>
    ///     Saga 持久化配置
    /// </summary>
    public SagaOptions Saga { get; set; } = new();

    /// <summary>
    ///     事件溯源配置
    /// </summary>
    public EventSourcingOptions EventSourcing { get; set; } = new();
}

public class LocalEventOptions
{
    /// <summary>
    ///     本地事件有界队列容量。队列满时发布方异步等待，不丢事件。
    /// </summary>
    public int Capacity { get; set; } = 1024;
}

/// <summary>
///     Saga 配置选项
/// </summary>
public class SagaOptions
{
    /// <summary>
    ///     默认关闭，避免仅启用 RabbitMQ 时意外初始化 Saga 存储。
    /// </summary>
    public bool Enabled { get; set; }

    public SagaRepositoryType RepositoryType { get; set; } = SagaRepositoryType.MongoDb;

    public MongoDbSagaOptions MongoDb { get; set; } = new();
}

public class MongoDbSagaOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:17017";
    public string DatabaseName { get; set; } = "sagas";
    public string? CollectionName { get; set; }
}

/// <summary>
///     事件溯源配置选项
/// </summary>
public class EventSourcingOptions
{
    /// <summary>
    ///     默认关闭，只有明确配置后才创建独立 Redis 连接。
    /// </summary>
    public bool Enabled { get; set; }

    public string RedisConnectionString { get; set; } = "localhost:6379";

    public string StreamPrefix { get; set; } = "events:";

    /// <summary>
    ///     Redis Stream 分页读取大小，避免回放时一次加载完整事件流。
    /// </summary>
    public int ReadBatchSize { get; set; } = 256;
}
