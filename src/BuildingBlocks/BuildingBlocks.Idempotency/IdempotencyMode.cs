namespace BuildingBlocks.Idempotency;

/// <summary>
///     重复请求的处理方式。
/// </summary>
public enum IdempotencyMode
{
    /// <summary>
    ///     首次成功响应会被保存，后续相同请求直接重放该响应。
    /// </summary>
    Replay,

    /// <summary>
    ///     首次成功后记录完成标记，后续相同请求返回冲突。
    /// </summary>
    Reject,

    /// <summary>
    ///     仅在请求执行期间持有资源锁，并发请求返回冲突，完成后允许再次执行。
    /// </summary>
    Lock
}
