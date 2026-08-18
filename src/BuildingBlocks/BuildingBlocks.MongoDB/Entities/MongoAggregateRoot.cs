using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.MongoDB.Entities;

/// <summary>表示可由通用 MongoDB 写仓储持久化的聚合根基类。</summary>
public abstract class MongoAggregateRoot : MongoEntity, IAggregateRoot
{
}
