using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BuildingBlocks.MongoDB;

public abstract class MongoEntity
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    public long? CreatorId { get; set; }
}