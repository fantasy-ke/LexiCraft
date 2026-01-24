using BuildingBlocks.Domain.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BuildingBlocks.MongoDB;

public abstract class MongoAggregateRoot : MongoEntity, IAggregateRoot
{
}
