using MemoryPack;

namespace BuildingBlocks.Caching.Redis.Serialization;

/// <summary>
///     MemoryPack 缓存序列化静态帮助类
/// </summary>
/// <remarks>
///     仅在 <see cref="Options.CacheServiceOptions.EnableBinarySerialization"/> 为 <see langword="true"/> 时使用。
///     收益是体积和 CPU 都明显优于 JSON；代价是要求类型标注 <c>MemoryPackable</c> 且成员布局稳定，
///     产物不可读、无法用 redis-cli 直接排查。存在二进制兼容风险：调整成员顺序、类型或删除成员后，
///     旧缓存字节会解析失败或产生错误数据，且与 JSON 格式互不兼容。切换序列化方式或修改契约时
///     必须同时更换缓存键前缀或清理旧键，不能依赖 TTL 自然过期。
/// </remarks>
internal static class MemoryPackCacheSerializer
{
    /// <summary>
    ///     序列化对象为字节数组
    /// </summary>
    /// <typeparam name="T">对象类型；必须满足 MemoryPack 的序列化要求。</typeparam>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的字节数组；<paramref name="value"/> 为 <see langword="null"/> 时返回空数组。</returns>
    /// <exception cref="InvalidOperationException">类型未注册为 MemoryPack 可序列化或序列化失败时抛出。</exception>
    public static byte[] Serialize<T>(T value)
    {
        if (value == null)
            return Array.Empty<byte>();

        try
        {
            return MemoryPackSerializer.Serialize(value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MemoryPack 序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     从字节数组反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型；必须与写入时的 MemoryPack 契约二进制兼容。</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的对象；输入为 <see langword="null"/> 或空数组时返回 <see langword="default"/>。</returns>
    /// <exception cref="InvalidOperationException">字节与当前类型契约不兼容或反序列化失败时抛出。</exception>
    public static T? Deserialize<T>(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return default;

        try
        {
            return MemoryPackSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"MemoryPack 反序列化失败: {ex.Message}", ex);
        }
    }
}