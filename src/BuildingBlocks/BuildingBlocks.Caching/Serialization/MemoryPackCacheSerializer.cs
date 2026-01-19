using System;
using MemoryPack;

namespace BuildingBlocks.Caching.Serialization;

/// <summary>
/// MemoryPack 缓存序列化静态帮助类
/// </summary>
public static class MemoryPackCacheSerializer
{
    /// <summary>
    /// 序列化对象为字节数组
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的字节数组</returns>
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
    /// 从字节数组反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的对象</returns>
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
