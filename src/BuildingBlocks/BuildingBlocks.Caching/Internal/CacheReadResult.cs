namespace BuildingBlocks.Caching.Internal;

/// <summary>
///     表示一次缓存读取是否命中以及命中时的值，避免用 default(T) 推断命中状态。
/// </summary>
internal readonly record struct CacheReadResult<T>(bool Found, T? Value)
{
    public static CacheReadResult<T> Miss => new(false, default);

    public static CacheReadResult<T> Hit(T? value) => new(true, value);
}
