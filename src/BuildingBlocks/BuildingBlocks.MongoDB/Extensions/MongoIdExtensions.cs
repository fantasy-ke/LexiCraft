using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using MongoDB.Bson;

namespace BuildingBlocks.MongoDB.Extensions;

/// <summary>提供 MongoDB ObjectId 字符串校验与按标识查询扩展。</summary>
public static class MongoIdExtensions
{
    /// <summary>判断字符串是否为有效的 MongoDB ObjectId。</summary>
    /// <param name="id">待校验字符串。</param>
    /// <returns>字符串非空且可解析为 <see cref="ObjectId"/> 时为 <see langword="true"/>。</returns>
    public static bool IsValidMongoId(this string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _);
    }

    /// <summary>校验并返回 MongoDB ObjectId 字符串。</summary>
    /// <param name="id">待校验字符串。</param>
    /// <param name="paramName">无效时写入异常的参数名。</param>
    /// <returns>通过校验的原始字符串。</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> 为空或不是有效 ObjectId 时抛出。</exception>
    public static string EnsureValidMongoId(this string? id, string paramName)
    {
        if (!id.IsValidMongoId())
            throw new ArgumentException("Id must be a valid Mongo ObjectId.", paramName);

        return id!;
    }

    /// <summary>把字符串解析为 ObjectId，并异步查询第一个匹配实体。</summary>
    /// <typeparam name="TEntity">MongoDB 实体类型。</typeparam>
    /// <param name="repository">查询仓储。</param>
    /// <param name="id">ObjectId 字符串。</param>
    /// <param name="cancellationToken">用于取消查询的令牌。</param>
    /// <returns>匹配实体，或 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> 不是有效 ObjectId 时抛出。</exception>
    public static Task<TEntity?> FirstOrDefaultByIdAsync<TEntity>(
        this IQueryRepository<TEntity> repository,
        string id,
        CancellationToken cancellationToken = default)
        where TEntity : MongoEntity
    {
        var validId = id.EnsureValidMongoId(nameof(id));
        var objectId = ObjectId.Parse(validId);
        return repository.FirstOrDefaultAsync(entity => entity.Id == objectId, cancellationToken);
    }

    /// <summary>按 ObjectId 字符串异步获取实体。</summary>
    /// <typeparam name="TEntity">MongoDB 实体类型。</typeparam>
    /// <param name="repository">查询仓储。</param>
    /// <param name="id">ObjectId 字符串。</param>
    /// <param name="cancellationToken">用于取消查询的令牌。</param>
    /// <returns>匹配实体，或 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> 不是有效 ObjectId 时抛出。</exception>
    /// <remarks>这是 <see cref="FirstOrDefaultByIdAsync{TEntity}"/> 的语义化别名。</remarks>
    public static Task<TEntity?> GetByIdAsync<TEntity>(
        this IQueryRepository<TEntity> repository,
        string id,
        CancellationToken cancellationToken = default)
        where TEntity : MongoEntity
    {
        return repository.FirstOrDefaultByIdAsync(id, cancellationToken);
    }
}
