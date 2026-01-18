using System;

namespace BuildingBlocks.Caching.Serialization;

/// <summary>
/// 缓存序列化器接口
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    /// 序列化对象为字节数组
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] Serialize<T>(T value);

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="data">字节数组</param>
    /// <returns>反序列化后的对象</returns>
    T? Deserialize<T>(byte[] data);

    /// <summary>
    /// 序列化器类型名称
    /// </summary>
    string Name { get; }
}