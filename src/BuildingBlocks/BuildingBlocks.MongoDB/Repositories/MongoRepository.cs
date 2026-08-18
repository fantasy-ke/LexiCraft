using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Repositories;

/// <summary>在统一 MongoDB 查询管线之上提供聚合根的即时写入操作。</summary>
/// <typeparam name="TEntity">MongoDB 聚合根类型。</typeparam>
/// <remarks>
///     插入、替换和删除命令在方法返回前已发送到数据库，不使用 <c>SaveChanges</c> 延迟提交。
///     当前实现不提供通用软删除；两个删除重载均为物理删除。
/// </remarks>
public class MongoRepository<TEntity> : MongoQueryRepository<TEntity>, IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    /// <summary>使用显式集合名创建可供业务仓储继承的写仓储。</summary>
    /// <param name="context">作用域 MongoDB 上下文。</param>
    /// <param name="resilienceService">事务外读取弹性服务。</param>
    /// <param name="performanceMonitor">仓储操作性能监控器。</param>
    /// <param name="collectionName">集合名；为空时使用实体 CLR 类型名。</param>
    protected MongoRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        string? collectionName = null)
        : base(context, resilienceService, performanceMonitor, collectionName)
    {
    }

    /// <summary>使用实体 CLR 类型名作为集合名创建写仓储。</summary>
    /// <param name="context">作用域 MongoDB 上下文。</param>
    /// <param name="resilienceService">事务外读取弹性服务。</param>
    /// <param name="performanceMonitor">仓储操作性能监控器。</param>
    public MongoRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor)
        : this(context, resilienceService, performanceMonitor, null)
    {
    }

    /// <inheritdoc />
    /// <remarks>写入前把 <see cref="MongoEntity.CreationTime"/> 覆盖为当前 UTC 时间。</remarks>
    public virtual async Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        entity.CreationTime = DateTime.UtcNow;
        await ExecuteWriteOperationAsync(
            "Insert",
            session => session is null
                ? Collection.InsertOneAsync(entity, cancellationToken: cancellationToken)
                : Collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken),
            cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    /// <remarks>序列只物化一次；空序列不发送数据库命令，所有文档共享同一 UTC 创建时间。</remarks>
    public virtual async Task InsertAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList) entity.CreationTime = now;

        await ExecuteWriteOperationAsync(
            "InsertMany",
            session => session is null
                ? Collection.InsertManyAsync(entityList, cancellationToken: cancellationToken)
                : Collection.InsertManyAsync(session, entityList, cancellationToken: cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>该操作按 <see cref="MongoEntity.Id"/> 替换整份文档；没有匹配文档时抛出异常。</remarks>
    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        var result = await ExecuteWriteOperationAsync(
            "Update",
            session => session is null
                ? Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken)
                : Collection.ReplaceOneAsync(session, filter, entity, cancellationToken: cancellationToken),
            cancellationToken);

        return result.MatchedCount > 0
            ? entity
            : throw new InvalidOperationException($"Failed to update entity with ID: {entity.Id}");
    }

    /// <inheritdoc />
    /// <remarks>按实体 ObjectId 执行单文档物理删除；未匹配文档时正常完成。</remarks>
    public virtual Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        return ExecuteWriteOperationAsync(
            "Delete",
            session => session is null
                ? Collection.DeleteOneAsync(filter, cancellationToken)
                : Collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWriteOperationAsync(
            "DeleteMany",
            session => session is null
                ? Collection.DeleteManyAsync(predicate, cancellationToken)
                : Collection.DeleteManyAsync(session, predicate, cancellationToken: cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>MongoDB 写操作即时生效，因此该兼容方法固定返回 0，不代表受影响文档数。</remarks>
    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}
