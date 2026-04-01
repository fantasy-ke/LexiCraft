using BuildingBlocks.Domain;
using MongoDB.Bson;

namespace BuildingBlocks.MongoDB;

public static class MongoIdExtensions
{
    public static bool IsValidMongoId(this string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _);
    }

    public static string EnsureValidMongoId(this string? id, string paramName)
    {
        if (!id.IsValidMongoId())
            throw new ArgumentException("Id must be a valid Mongo ObjectId.", paramName);

        return id!;
    }

    public static Task<TEntity?> FirstOrDefaultByIdAsync<TEntity>(this IQueryRepository<TEntity> repository, string id)
        where TEntity : MongoEntity
    {
        var validId = id.EnsureValidMongoId(nameof(id));
        var objectId = ObjectId.Parse(validId);
        return repository.FirstOrDefaultAsync(entity => entity.Id == objectId);
    }

    public static Task<TEntity?> GetByIdAsync<TEntity>(this IQueryRepository<TEntity> repository, string id)
        where TEntity : MongoEntity
    {
        return repository.FirstOrDefaultByIdAsync(id);
    }
}


