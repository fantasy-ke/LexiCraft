using BuildingBlocks.Caching.Options;

namespace BuildingBlocks.Caching.Abstractions;

/// <summary>
///     提供本地内存缓存、Redis 分布式缓存及防击穿重建的统一访问入口。
/// </summary>
/// <remarks>
///     每次调用通过 <see cref="CacheServiceOptions"/> 独立选择缓存层、命名 Redis 实例、序列化方式及错误策略。
///     公共读取 API 以 <see langword="default"/> 表示未命中或隐藏错误后的无值结果；组件内部使用显式命中状态，
///     因而已缓存的 <c>0</c>、<see langword="false"/> 或 <see langword="null"/> 不会被误判并触发重复重建。
/// </remarks>
public interface ICacheService
{
    /// <summary>
    ///     获取缓存值；未命中时调用工厂，并在工厂结果非 <see langword="null"/> 时写入已启用的缓存层。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="factory">
    ///     缓存未命中时生成值的异步工厂。该委托本身不接收取消令牌；需要取消时应在闭包中传入
    ///     <paramref name="cancellationToken"/>。
    /// </param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；不会修改全局配置。</param>
    /// <param name="cancellationToken">用于取消缓存 I/O、锁等待及调用链中显式捕获的操作。</param>
    /// <returns>命中的缓存值、工厂生成的值，或错误策略产生的降级值。</returns>
    /// <remarks>
    ///     启用锁时，获得锁后会按相同选项再次读取缓存。锁获取失败且
    ///     <see cref="CacheServiceOptions.FallbackToFactory"/> 为 <see langword="true"/> 时，工厂仍可能并发执行。
    ///     <see cref="OperationCanceledException"/> 不受 <see cref="CacheServiceOptions.HideErrors"/> 影响并始终向上传播。
    /// </remarks>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     将值写入本次调用启用的缓存层。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="value">缓存值。</param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；不会修改全局配置。</param>
    /// <param name="cancellationToken">用于取消 Redis 写入；取消不会被错误隐藏策略吞掉。</param>
    /// <remarks>
    ///     分布式值按选项序列化并使用动态调整后的 TTL；本地值直接保存在当前进程，使用
    ///     <see cref="CacheServiceOptions.LocalExpiry"/> 或继承 <see cref="CacheServiceOptions.Expiry"/>。
    ///     当 <see cref="CacheServiceOptions.HideErrors"/> 为 <see langword="true"/> 时，依赖错误可被记录并隐藏。
    /// </remarks>
    Task SetAsync<T>(string key, T value, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     从本地缓存优先、Redis 次之地获取值。
    /// </summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；不会修改全局配置。</param>
    /// <param name="cancellationToken">用于取消 Redis 读取；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>缓存值、配置的降级值，或未命中时的 <see langword="default"/>。</returns>
    /// <remarks>
    ///     Redis 命中后可回填本地缓存。公共返回值无法区分“未命中”和缓存值恰为
    ///     <see langword="default"/>；需要自动重建时应使用 <see cref="GetOrSetAsync{T}"/>，其内部保留显式命中状态。
    /// </remarks>
    Task<T?> GetAsync<T>(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除本次调用启用的 Redis 键和本地缓存副本。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；必须与写入时的 Redis 实例选择一致。</param>
    /// <param name="cancellationToken">用于取消 Redis 删除；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>Redis 删除结果；未启用 Redis 时，本地删除完成后返回 <see langword="true"/>。</returns>
    /// <remarks>本地缓存键包含命名 Redis 实例，因此使用不同实例选项删除不会影响另一实例对应的本地副本。</remarks>
    Task<bool> RemoveAsync(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查本次调用启用的缓存层是否包含指定键。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="configure">用于修改本次调用默认选项的新委托。</param>
    /// <param name="cancellationToken">用于取消 Redis 查询；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>任一已启用缓存层存在该键时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> ExistsAsync(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     修改 Redis 键的剩余生存时间。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="expirationTime">从当前时刻起计算的 Redis 生存时间。</param>
    /// <param name="configure">用于选择 Redis 实例和错误策略的本次调用选项。</param>
    /// <param name="cancellationToken">用于取消 Redis 操作；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>Redis 已为现有键设置 TTL 时为 <see langword="true"/>；未启用 Redis、键不存在或隐藏错误时为 <see langword="false"/>。</returns>
    /// <remarks>此操作不会修改当前进程中已存在本地缓存项的过期时间。</remarks>
    Task<bool> SetExpirationAsync(string key, TimeSpan expirationTime, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     从 Redis Hash 读取指定字段并解析为强类型；未命中或逻辑过期时构建并写入完整 Hash。
    /// </summary>
    /// <typeparam name="TResult">解析后的返回值类型。</typeparam>
    /// <param name="hashKey">Redis Hash 键。</param>
    /// <param name="queryFields">需要返回的业务字段；组件会额外读取内部时间戳字段以验证逻辑有效期。</param>
    /// <param name="parseFromHash">将已读取字段字典解析为返回值的同步函数。</param>
    /// <param name="buildFullCache">
    ///     缓存不存在或过期时构建完整字段集合的异步工厂。该委托不接收取消令牌，调用方应按需通过闭包传递。
    /// </param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；Hash 操作仅使用 Redis，不使用本地缓存。</param>
    /// <param name="cancellationToken">用于取消 Redis I/O 和锁等待；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>解析后的命中值、重建值、降级值，或无可用值时的 <see langword="default"/>。</returns>
    /// <remarks>
    ///     重建时组件保留名称为 <c>cache_timestamp</c> 的字段并以 UTC ISO 8601 时间写入；业务数据不得使用该名称。
    ///     Hash 字段写入与 Redis TTL 在同一事务中提交。逻辑有效期使用 <see cref="CacheServiceOptions.Expiry"/>；
    ///     动态 Hash TTL 仅调整 Redis 键的物理 TTL。缺失或无法解析的内部时间戳按有效缓存处理。
    /// </remarks>
    Task<TResult?> GetOrSetHashAsync<TResult>(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     从 Redis Hash 读取指定字段；未命中或逻辑过期时构建并写入完整 Hash。
    /// </summary>
    /// <param name="hashKey">Redis Hash 键。</param>
    /// <param name="queryFields">需要返回的业务字段；组件会额外读取内部时间戳字段。</param>
    /// <param name="buildFullCache">缓存不存在或过期时构建完整字段集合的异步工厂。</param>
    /// <param name="configure">用于修改本次调用默认选项的新委托；Hash 操作仅使用 Redis。</param>
    /// <param name="cancellationToken">用于取消 Redis I/O 和锁等待；取消不会被错误隐藏策略吞掉。</param>
    /// <returns>命中的字段、重建后的完整字段、降级值，或无可用值时的 <see langword="null"/>。</returns>
    /// <remarks>
    ///     返回字典可能包含内部 <c>cache_timestamp</c> 字段。时间戳、事务 TTL 和锁语义与强类型重载相同。
    /// </remarks>
    Task<Dictionary<string, string>?> GetOrSetHashAsync(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);
}