using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Extensions.System;

namespace BuildingBlocks.Caching.Redis.Serialization;

/// <summary>
///     JSON 缓存序列化静态帮助类
/// </summary>
/// <remarks>
///     组件的默认序列化方式，固定使用 camelCase 属性名、不缩进、忽略 <see langword="null"/> 写入。
///     选择 JSON 的代价是体积和 CPU 高于 MemoryPack，收益是可读、可用 redis-cli 直接排查，
///     并且对新增可选属性有较好的向前兼容性。序列化设置是内部常量，修改它等同于变更已有缓存数据格式。
/// </remarks>
internal static class JsonCacheSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    ///     序列化对象为字节数组
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的字节数组；<paramref name="value"/> 为 <see langword="null"/> 时返回空数组。</returns>
    /// <exception cref="InvalidOperationException">序列化失败时抛出，内部异常保留原因。</exception>
    public static byte[] Serialize<T>(T value)
    {
        if (value == null)
            return Array.Empty<byte>();

        try
        {
            return value.ToJsonBytes(DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON 序列化失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     从字节数组反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的对象；输入为 <see langword="null"/> 或空数组时返回 <see langword="default"/>。</returns>
    /// <exception cref="InvalidOperationException">
    ///     数据不是合法 JSON 或与目标类型不兼容时抛出；缓存值不会因此被静默丢弃。
    /// </exception>
    public static T? Deserialize<T>(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return default;

        try
        {
            return data.FromJson<T>(DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON 反序列化失败: {ex.Message}", ex);
        }
    }
}