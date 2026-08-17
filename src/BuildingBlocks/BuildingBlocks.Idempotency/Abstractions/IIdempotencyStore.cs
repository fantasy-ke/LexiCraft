namespace BuildingBlocks.Idempotency.Abstractions;

/// <summary>
///     定义幂等租约和已完成响应的持久化契约。
/// </summary>
/// <remarks>
///     存储实现必须保证租约获取、完成提交和放弃操作具备所有权校验，避免过期请求覆盖新请求的状态。
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    ///     尝试获取指定请求的处理租约，或读取该请求当前的幂等状态。
    /// </summary>
    /// <param name="key">服务端生成的幂等存储键，而不是客户端原始请求头。</param>
    /// <param name="fingerprint">用于验证请求方法、路径、查询和请求体是否一致的指纹。</param>
    /// <param name="processingTimeout">租约的最长有效期。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>租约获取状态，以及状态对应的租约或已保存响应。</returns>
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        string fingerprint,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     在租约仍属于当前请求时，原子保存完成结果并释放租约。
    /// </summary>
    /// <param name="lease">首次获取成功时返回的租约。</param>
    /// <param name="response">需要保存的响应状态和可选响应体。</param>
    /// <param name="retention">完成结果的保留时间。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>租约所有权校验通过且完成结果已保存时返回 <see langword="true"/>。</returns>
    Task<bool> CompleteAsync(
        IdempotencyLease lease,
        IdempotencyStoredResponse response,
        TimeSpan retention,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     在租约仍属于当前请求时释放租约，不保存完成结果。
    /// </summary>
    /// <param name="lease">需要释放的租约。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>租约所有权校验通过且租约已释放时返回 <see langword="true"/>。</returns>
    Task<bool> AbandonAsync(
        IdempotencyLease lease,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     描述一次幂等状态获取的结果。
/// </summary>
public enum IdempotencyAcquireStatus
{
    /// <summary>
    ///     当前请求已获得执行租约，可以调用后续业务逻辑。
    /// </summary>
    Acquired,

    /// <summary>
    ///     相同请求正在由其他请求处理。
    /// </summary>
    InProgress,

    /// <summary>
    ///     相同请求已经完成，并可能携带可重放响应。
    /// </summary>
    Completed,

    /// <summary>
    ///     同一幂等键已绑定到不同的请求指纹。
    /// </summary>
    FingerprintMismatch
}

/// <summary>
///     表示某个请求对幂等处理状态的临时所有权。
/// </summary>
/// <param name="Key">服务端生成的幂等存储键。</param>
/// <param name="Fingerprint">本次请求的请求指纹。</param>
/// <param name="OwnerToken">区分租约持有者的随机所有权令牌。</param>
public sealed record IdempotencyLease(string Key, string Fingerprint, string OwnerToken);

/// <summary>
///     表示已完成请求可供后续重复请求使用的响应信息。
/// </summary>
/// <param name="StatusCode">原始响应状态码。</param>
/// <param name="ContentType">原始响应内容类型。</param>
/// <param name="Body">在大小限制内捕获的原始响应体。</param>
/// <param name="Replayable">是否允许将响应体重放给后续请求。</param>
public sealed record IdempotencyStoredResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    bool Replayable);

/// <summary>
///     表示存储层返回的幂等状态及其关联数据。
/// </summary>
/// <param name="Status">当前幂等状态。</param>
/// <param name="Lease"><see cref="IdempotencyAcquireStatus.Acquired"/> 状态对应的执行租约。</param>
/// <param name="Response"><see cref="IdempotencyAcquireStatus.Completed"/> 状态对应的已保存响应。</param>
public sealed record IdempotencyAcquireResult(
    IdempotencyAcquireStatus Status,
    IdempotencyLease? Lease = null,
    IdempotencyStoredResponse? Response = null);