namespace BuildingBlocks.Caching.Internal;

/// <summary>
///     表示一次缓存读取是否命中以及命中时的值，避免用 default(T) 推断命中状态。
/// </summary>
/// <typeparam name="T">缓存值类型。</typeparam>
/// <param name="Found">是否在缓存中找到该键。</param>
/// <param name="Value">命中时的缓存值；未命中时为 <see langword="default"/>。</param>
/// <remarks>
///     公共 <see cref="Abstractions.ICacheService"/> 的读取方法只返回值本身，因此无法区分“未命中”和
///     “命中且值恰为 <see langword="default"/>”。组件内部一律传递该结构，使已缓存的 <c>0</c>、
///     <see langword="false"/> 或 <see langword="null"/> 不会被误判为未命中并触发重复重建。
/// </remarks>
internal readonly record struct CacheReadResult<T>(bool Found, T? Value)
{
    /// <summary>
    ///     获取表示未命中的结果。
    /// </summary>
    public static CacheReadResult<T> Miss => new(false, default);

    /// <summary>
    ///     创建表示命中的结果。
    /// </summary>
    /// <param name="value">命中的缓存值，允许为 <see langword="null"/>。</param>
    /// <returns><see cref="Found"/> 为 <see langword="true"/> 的读取结果。</returns>
    public static CacheReadResult<T> Hit(T? value) => new(true, value);
}
