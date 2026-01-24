using BuildingBlocks.Domain.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BuildingBlocks.MongoDB;

public abstract class MongoAggregateRoot : IAggregateRoot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public DateTime CreationTime { get; set; } = DateTime.Now;

    public long? CreatorId { get; set; }
}
