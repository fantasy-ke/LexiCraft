using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BuildingBlocks.MongoDB.Entities;

/// <summary>提供 MongoDB 文档的 ObjectId、UTC 创建时间和可选创建者标识。</summary>
public abstract class MongoEntity
{
    /// <summary>获取或设置文档主键；新实例默认生成一个 <see cref="ObjectId"/>。</summary>
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>获取或设置 UTC 创建时间；写仓储插入时会覆盖为当前 UTC 时间。</summary>
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    /// <summary>获取或设置可选的长整型创建者标识。</summary>
    public long? CreatorId { get; set; }
}
