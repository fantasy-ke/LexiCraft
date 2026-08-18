using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace BuildingBlocks.MongoDB.Serialization;

/// <summary>为 <see cref="DateTime"/> 提供强制 UTC DateTimeKind 的 BSON 序列化器。</summary>
/// <remarks>该提供程序由 MongoDB 注册扩展作为进程级全局映射安装。</remarks>
public class DateTimeSerializationProvider : IBsonSerializationProvider
{
    /// <summary>按类型选择 BSON 序列化器。</summary>
    /// <param name="type">待序列化 CLR 类型。</param>
    /// <returns><see cref="DateTime"/> 的 UTC 序列化器；其他类型返回 <see langword="null"/> 交由后续提供程序处理。</returns>
    public IBsonSerializer? GetSerializer(Type type)
    {
        return type == typeof(DateTime) ? new DateTimeSerializer(DateTimeKind.Utc) : null;
    }
}