namespace BuildingBlocks.Idempotency;

/// <summary>
///     为控制器 Action 或端点声明幂等策略。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IdempotentAttribute : Attribute
{
    public IdempotentAttribute(IdempotencyMode mode = IdempotencyMode.Replay)
    {
        Mode = mode;
    }

    public IdempotencyMode Mode { get; }

    /// <summary>
    ///     是否强制客户端提供 Idempotency-Key 请求头。
    /// </summary>
    public bool RequireKey { get; set; }

    /// <summary>
    ///     成功记录保留秒数。0 表示使用全局配置。
    /// </summary>
    public int RetentionSeconds { get; set; }

    /// <summary>
    ///     执行中租约秒数。0 表示使用全局配置。
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; }

    /// <summary>
    ///     Replay 模式等待首个请求完成的毫秒数。0 表示使用全局配置。
    /// </summary>
    public int ReplayWaitTimeoutMilliseconds { get; set; }
}
