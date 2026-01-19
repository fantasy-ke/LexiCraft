using System;
using System.Text;
using System.Text.Json;

namespace BuildingBlocks.Caching.Serialization;

/// <summary>
/// JSON 缓存序列化静态帮助类
/// </summary>
public static class JsonCacheSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

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
            var json = JsonSerializer.Serialize(value, DefaultOptions);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON 序列化失败: {ex.Message}", ex);
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
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON 反序列化失败: {ex.Message}", ex);
        }
    }
}
