using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Options;

namespace BuildingBlocks.Caching.Redis;

/// <summary>
///     CacheService 内部使用的 Redis 存储边界。
/// </summary>
/// <remarks>
///     该接口只描述单次 Redis 操作，不包含本地内存缓存、防击穿锁和错误降级；这些语义由
///     <see cref="Services.CacheService"/> 编排。实现不吞掉依赖异常：Redis 连接失败、超时和序列化失败
///     一律向上抛出，是否降级由 <see cref="CacheServiceOptions.HideErrors"/> 在上层决定。
///     所有方法通过 <c>WaitAsync</c> 响应取消，取消只中断本地等待，不撤销已发送到 Redis 的命令。
/// </remarks>
internal interface IRedisCacheStore
{
    /// <summary>
    ///     读取 Redis String 值并按选项反序列化，返回显式命中状态。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">Redis 键。</param>
    /// <param name="options">提供实例名、序列化和压缩设置的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <returns>命中时为 <see cref="CacheReadResult{T}.Hit"/>，键不存在时为 <see cref="CacheReadResult{T}.Miss"/>。</returns>
    /// <remarks>返回显式命中标记，使已缓存的 <c>0</c>、<see langword="false"/> 不会被误判为未命中。</remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">反序列化或解压失败时抛出。</exception>
    Task<CacheReadResult<T>> GetAsync<T>(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按选项序列化后写入 Redis String 值并设置 TTL。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">Redis 键。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="options">提供实例名、序列化和默认 TTL 的本次调用选项。</param>
    /// <param name="expiry">显式 TTL；为 <see langword="null"/> 时使用 <see cref="CacheServiceOptions.Expiry"/>。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">序列化或压缩失败时抛出。</exception>
    Task SetAsync<T>(
        string key,
        T value,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除 Redis 键。
    /// </summary>
    /// <param name="key">Redis 键。</param>
    /// <param name="options">提供实例名的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <returns>删除了现有键时为 <see langword="true"/>；键原本不存在时为 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    Task<bool> RemoveAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查 Redis 键是否存在。
    /// </summary>
    /// <param name="key">Redis 键。</param>
    /// <param name="options">提供实例名的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <returns>键存在时为 <see langword="true"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    Task<bool> ExistsAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     重设 Redis 键的剩余生存时间。
    /// </summary>
    /// <param name="key">Redis 键。</param>
    /// <param name="expiry">从当前时刻起计算的新 TTL。</param>
    /// <param name="options">提供实例名的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <returns>已为现有键设置 TTL 时为 <see langword="true"/>；键不存在时为 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    Task<bool> SetExpirationAsync(
        string key,
        TimeSpan expiry,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     读取 Redis Hash 的指定字段。
    /// </summary>
    /// <param name="key">Redis Hash 键。</param>
    /// <param name="fields">要读取的字段名集合。</param>
    /// <param name="options">提供实例名的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <returns>
    ///     字段名到字符串值的字典，只包含实际存在的字段；Hash 键本身不存在时为 <see langword="null"/>；
    ///     字段集合为空时为空字典。
    /// </returns>
    /// <remarks>
    ///     所有请求字段都缺失时会额外执行一次 <c>EXISTS</c>，用于区分“键不存在”与“键存在但字段缺失”。
    ///     Hash 字段值不参与序列化和压缩，始终按字符串存取。
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    Task<Dictionary<string, string>?> HashGetAsync(
        string key,
        IEnumerable<string> fields,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     在同一 Redis 事务中写入 Hash 字段并设置整个 Hash 键的 TTL。
    /// </summary>
    /// <param name="key">Redis Hash 键。</param>
    /// <param name="values">要写入的字段名到字符串值的映射，不能为空集合。</param>
    /// <param name="options">提供实例名和默认 TTL 的本次调用选项。</param>
    /// <param name="expiry">显式 TTL；为 <see langword="null"/> 时使用 <see cref="CacheServiceOptions.Expiry"/>。</param>
    /// <param name="cancellationToken">用于取消等待 Redis 响应。</param>
    /// <remarks>
    ///     Redis 的 TTL 只能作用于整个 key，无法为单个 field 设置过期时间：任一字段的写入都会把整个 Hash 的
    ///     TTL 重置为 <paramref name="expiry"/>，且字段不会单独过期。逻辑上的字段级有效期只能靠额外的
    ///     时间戳字段判断。<c>HashSet</c> 与 <c>KeyExpire</c> 在同一事务中提交，避免出现写入了数据却没有 TTL 的永久键。
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空或空白，或 <paramref name="values"/> 为空集合时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">事务提交失败或 TTL 设置未生效时抛出。</exception>
    Task HashSetAsync(
        string key,
        Dictionary<string, string> values,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
