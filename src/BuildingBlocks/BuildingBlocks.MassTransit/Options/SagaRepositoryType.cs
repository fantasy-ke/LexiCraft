namespace BuildingBlocks.MassTransit.Options;

/// <summary>
///     Saga 存储类型枚举
/// </summary>
public enum SagaRepositoryType
{
    /// <summary>
    ///     MongoDB 存储
    /// </summary>
    MongoDb,

    /// <summary>
    ///     Redis 存储 (暂未实现或已移除支持)
    /// </summary>
    Redis,

    /// <summary>
    ///     内存存储 (仅用于测试)
    /// </summary>
    InMemory
}