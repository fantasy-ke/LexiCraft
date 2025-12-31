using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace BuildingBlocks.MongoDB.Serialization;

public class DateTimeSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer? GetSerializer(Type type)
    {
        return type == typeof(DateTime) ? new DateTimeSerializer(DateTimeKind.Local) : null;
    }
}
