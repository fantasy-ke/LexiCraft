using System.Linq.Expressions;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Repositories;

/// <summary>通过统一的 session、性能监控和读取弹性管线访问 MongoDB 集合。</summary>
/// <typeparam name="TEntity">派生自 <see cref="MongoEntity"/> 的文档类型。</typeparam>
/// <remarks>
///     事务外读取可由 <see cref="IMongoResilienceService"/> 重试；事务内读取必须绑定当前 session 且不局部重试。
///     MongoDB 没有 EF Core ChangeTracker，因此 <see cref="Query"/> 与 <see cref="QueryNoTracking"/> 等价。
/// </remarks>
public class MongoQueryRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : MongoEntity
{
    /// <summary>使用显式集合名创建可供业务仓储继承的查询仓储。</summary>
    /// <param name="context">提供数据库与当前事务 session 的作用域上下文。</param>
    /// <param name="resilienceService">仅用于事务外读取的 MongoDB 弹性服务。</param>
    /// <param name="performanceMonitor">仓储操作性能监控器。</param>
    /// <param name="collectionName">集合名；为空时使用实体 CLR 类型名。</param>
    protected MongoQueryRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        string? collectionName = null)
    {
        Context = context;
        ResilienceService = resilienceService;
        PerformanceMonitor = performanceMonitor;
        CollectionName = collectionName ?? typeof(TEntity).Name;
        Collection = context.Database.GetCollection<TEntity>(CollectionName);
    }

    /// <summary>使用实体 CLR 类型名作为集合名创建查询仓储。</summary>
    /// <param name="context">提供数据库与当前事务 session 的作用域上下文。</param>
    /// <param name="resilienceService">仅用于事务外读取的 MongoDB 弹性服务。</param>
    /// <param name="performanceMonitor">仓储操作性能监控器。</param>
    public MongoQueryRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor)
        : this(context, resilienceService, performanceMonitor, null)
    {
    }

    /// <summary>获取作用域 MongoDB 上下文。</summary>
    protected IMongoDbContext Context { get; }

    /// <summary>获取事务外读取弹性服务。</summary>
    protected IMongoResilienceService ResilienceService { get; }

    /// <summary>获取性能监控器。</summary>
    protected IMongoPerformanceMonitor PerformanceMonitor { get; }

    /// <summary>获取当前实体的 MongoDB 集合。</summary>
    protected IMongoCollection<TEntity> Collection { get; }

    /// <summary>获取实际集合名称。</summary>
    protected string CollectionName { get; }

    /// <inheritdoc />
    public virtual Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return FindAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "FirstOrDefault",
            predicate,
            query => query.Limit(1).FirstOrDefaultAsync(cancellationToken),
            cancellationToken)!;
    }

    /// <inheritdoc />
    public virtual Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "First",
            predicate,
            query => query.Limit(1).FirstAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "SingleOrDefault",
            predicate,
            query => query.Limit(2).SingleOrDefaultAsync(cancellationToken),
            cancellationToken)!;
    }

    /// <inheritdoc />
    public virtual Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Single",
            predicate,
            query => query.Limit(2).SingleAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var count = await ExecuteReadOperationAsync(
            "Count",
            session => CountDocumentsAsync(predicate, session, cancellationToken),
            cancellationToken);
        return checked((int)count);
    }

    /// <inheritdoc />
    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Any",
            predicate,
            query => query.Limit(1).AnyAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteReadOperationAsync(
            "FindAll",
            session => Find(Builders<TEntity>.Filter.Empty, session).ToListAsync(cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual IQueryable<TEntity> Query()
    {
        return AsQueryable();
    }

    /// <inheritdoc />
    public virtual IQueryable<TEntity> QueryNoTracking()
    {
        return AsQueryable();
    }

    /// <inheritdoc />
    /// <remarks>事务外并行执行计数和列表查询；事务内为遵守 session 约束而串行执行。</remarks>
    public virtual async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageIndex, pageSize);
        var skip = checked((pageIndex - 1) * pageSize);
        var (items, totalCount) =
            await FindPagedAsync(predicate, skip, pageSize, orderBy, !isAsc, cancellationToken);
        return (checked((int)totalCount), items);
    }

    /// <summary>通过统一读取管线返回满足条件的全部文档。</summary>
    /// <param name="filter">文档筛选表达式。</param>
    /// <param name="cancellationToken">用于取消查询和重试等待的令牌。</param>
    /// <returns>匹配文档列表。</returns>
    protected Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Find",
            filter,
            query => query.ToListAsync(cancellationToken),
            cancellationToken);
    }

    /// <summary>通过统一读取管线查询分页文档和总数。</summary>
    /// <param name="filter">可选筛选表达式；为空时匹配全部文档。</param>
    /// <param name="skip">要跳过的文档数。</param>
    /// <param name="limit">最多返回的文档数。</param>
    /// <param name="sortBy">可选排序表达式。</param>
    /// <param name="sortDescending">是否降序。</param>
    /// <param name="cancellationToken">用于取消查询和重试等待的令牌。</param>
    /// <returns>当前页文档及匹配总数。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="skip"/> 小于 0 或 <paramref name="limit"/> 小于等于 0 时抛出。</exception>
    protected Task<(List<TEntity> Items, long TotalCount)> FindPagedAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        int skip = 0,
        int limit = 20,
        Expression<Func<TEntity, object>>? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));

        var filterDefinition = filter is null
            ? Builders<TEntity>.Filter.Empty
            : Builders<TEntity>.Filter.Where(filter);

        return ExecuteReadOperationAsync(
            "FindPaged",
            async session =>
            {
                var query = Find(filterDefinition, session);
                if (sortBy is not null)
                    query = sortDescending ? query.SortByDescending(sortBy) : query.SortBy(sortBy);

                if (session is not null)
                {
                    // MongoDB does not support parallel operations on the same transaction session.
                    var totalCount = await CountDocumentsAsync(filterDefinition, session, cancellationToken);
                    var items = await query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);
                    return (items, totalCount);
                }

                var totalCountTask = CountDocumentsAsync(filterDefinition, null, cancellationToken);
                var itemsTask = query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);
                await Task.WhenAll(totalCountTask, itemsTask);
                return (await itemsTask, await totalCountTask);
            },
            cancellationToken);
    }

    /// <summary>执行受监控的读取，并仅在没有活动事务时应用弹性重试。</summary>
    /// <typeparam name="TResult">读取结果类型。</typeparam>
    /// <param name="operationName">性能指标和弹性操作名称。</param>
    /// <param name="operation">接收当前事务 session 或 <see langword="null"/> 的读取委托。</param>
    /// <param name="cancellationToken">用于取消读取及重试等待的令牌。</param>
    /// <returns>读取结果。</returns>
    /// <remarks>事务必须作为完整单元重试，不能在同一 session 内单独重试一条读取。</remarks>
    protected async Task<TResult> ExecuteReadOperationAsync<TResult>(
        string operationName,
        Func<IClientSessionHandle?, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        var session = GetActiveSession();

        if (session is not null)
            return await operation(session);

        return await ResilienceService.ExecuteWithRetryAsync(
            () => operation(null),
            $"{operationName}_{CollectionName}",
            cancellationToken);
    }

    /// <summary>执行返回结果的受监控写操作，不应用仓储级重试。</summary>
    /// <typeparam name="TResult">写操作结果类型。</typeparam>
    /// <param name="operationName">性能指标名称。</param>
    /// <param name="operation">接收当前事务 session 或 <see langword="null"/> 的写入委托。</param>
    /// <param name="cancellationToken">用于取消写命令的令牌。</param>
    /// <returns>写入结果。</returns>
    /// <remarks>写入依赖 MongoDB 驱动 retryable writes；业务仍须提供幂等键、唯一索引或事务保护。</remarks>
    protected async Task<TResult> ExecuteWriteOperationAsync<TResult>(
        string operationName,
        Func<IClientSessionHandle?, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        return await operation(GetActiveSession());
    }

    /// <summary>执行不返回结果的受监控写操作，不应用仓储级重试。</summary>
    /// <param name="operationName">性能指标名称。</param>
    /// <param name="operation">接收当前事务 session 或 <see langword="null"/> 的写入委托。</param>
    /// <param name="cancellationToken">用于取消写命令的令牌。</param>
    /// <returns>表示写入过程的任务。</returns>
    /// <remarks>存在活动事务时，委托必须使用传入 session，确保事务内所有操作共享同一会话。</remarks>
    protected async Task ExecuteWriteOperationAsync(
        string operationName,
        Func<IClientSessionHandle?, Task> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        await operation(GetActiveSession());
    }

    private Task<TResult> ExecuteFindAsync<TResult>(
        string operationName,
        Expression<Func<TEntity, bool>> predicate,
        Func<IFindFluent<TEntity, TEntity>, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return ExecuteReadOperationAsync(
            operationName,
            session => operation(Find(predicate, session)),
            cancellationToken);
    }

    private IFindFluent<TEntity, TEntity> Find(
        Expression<Func<TEntity, bool>> predicate,
        IClientSessionHandle? session)
    {
        return session is null ? Collection.Find(predicate) : Collection.Find(session, predicate);
    }

    private IFindFluent<TEntity, TEntity> Find(
        FilterDefinition<TEntity> filter,
        IClientSessionHandle? session)
    {
        return session is null ? Collection.Find(filter) : Collection.Find(session, filter);
    }

    private Task<long> CountDocumentsAsync(
        Expression<Func<TEntity, bool>> predicate,
        IClientSessionHandle? session,
        CancellationToken cancellationToken)
    {
        return session is null
            ? Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken)
            : Collection.CountDocumentsAsync(session, predicate, cancellationToken: cancellationToken);
    }

    private Task<long> CountDocumentsAsync(
        FilterDefinition<TEntity> filter,
        IClientSessionHandle? session,
        CancellationToken cancellationToken)
    {
        return session is null
            ? Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            : Collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);
    }

    private IQueryable<TEntity> AsQueryable()
    {
        var session = GetActiveSession();
        return session is null ? Collection.AsQueryable() : Collection.AsQueryable(session);
    }

    private IClientSessionHandle? GetActiveSession()
    {
        return Context.Session is { IsInTransaction: true } session ? session : null;
    }

    private static void ValidatePagination(int pageIndex, int pageSize)
    {
        if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    }
}
