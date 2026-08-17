namespace BuildingBlocks.Idempotency;

/// <summary>
///     为控制器、Action 或端点声明幂等策略。
/// </summary>
/// <remarks>
///     Minimal API 可以通过 <c>WithMetadata(new IdempotentAttribute(...))</c> 添加该元数据。
///     未提供幂等键且 <see cref="RequireKey"/> 为 <see langword="false"/> 时，请求会直接执行而不进入幂等流程。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IdempotentAttribute : Attribute
{
    /// <summary>
    ///     使用指定的重复请求处理模式创建幂等声明。
    /// </summary>
    /// <param name="mode">重复请求的处理方式，默认重放首次成功响应。</param>
    public IdempotentAttribute(IdempotencyMode mode = IdempotencyMode.Replay)
    {
        Mode = mode;
    }

    /// <summary>
    ///     获取重复请求的处理方式。
    /// </summary>
    public IdempotencyMode Mode { get; }

    /// <summary>
    ///     获取或设置是否强制客户端提供配置的幂等请求头。
    /// </summary>
    public bool RequireKey { get; set; }

    /// <summary>
    ///     获取或设置成功记录保留秒数。0 表示使用全局配置。
    /// </summary>
    public int RetentionSeconds { get; set; }

    /// <summary>
    ///     获取或设置执行中租约秒数。0 表示使用全局配置。
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; }

    /// <summary>
    ///     获取或设置 Replay 模式等待首个请求完成的毫秒数。0 表示使用全局配置。
    /// </summary>
    public int ReplayWaitTimeoutMilliseconds { get; set; }
}