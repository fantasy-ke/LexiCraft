using System.Linq.Expressions;

namespace BuildingBlocks.Persistence.Abstractions.Repositories;

/// <summary>
///     定义与具体数据库提供程序无关的实体只读仓储契约。
/// </summary>
/// <typeparam name="TEntity">要查询的实体类型。</typeparam>
/// <remarks>
///     表达式能否被翻译、查询是否跟踪实体以及事务可见性由实际提供程序决定。
///     所有异步方法都应将 <see cref="CancellationToken"/> 传递到底层驱动。
/// </remarks>
public interface IQueryRepository<TEntity>
    where TEntity : class
{
    /// <summary>异步返回满足条件的全部实体。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>包含全部匹配实体的列表；没有匹配项时返回空列表。</returns>
    Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步返回满足条件的第一个实体，找不到时返回 <see langword="null"/>。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>第一个匹配实体，或 <see langword="null"/>。</returns>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步返回满足条件的第一个实体。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>第一个匹配实体。</returns>
    /// <exception cref="InvalidOperationException">没有匹配实体时抛出。</exception>
    Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步返回唯一匹配实体，没有匹配项时返回 <see langword="null"/>。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>唯一匹配实体，或 <see langword="null"/>。</returns>
    /// <exception cref="InvalidOperationException">存在多个匹配实体时抛出。</exception>
    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步返回唯一匹配实体。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>唯一匹配实体。</returns>
    /// <exception cref="InvalidOperationException">没有匹配实体或存在多个匹配实体时抛出。</exception>
    Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步统计满足条件的实体数。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>匹配实体数。</returns>
    /// <exception cref="OverflowException">提供程序计数超过 <see cref="int.MaxValue"/> 时实现可抛出。</exception>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步判断是否存在满足条件的实体。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>存在匹配实体时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步获取一个满足条件的实体，找不到时返回 <see langword="null"/>。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>匹配实体，或 <see langword="null"/>。</returns>
    /// <remarks>当前内置提供程序采用“第一个或默认值”语义，不校验结果唯一性。</remarks>
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>异步返回仓储中的全部实体。</summary>
    /// <param name="cancellationToken">用于取消数据库操作的令牌。</param>
    /// <returns>实体列表；集合为空时返回空列表。</returns>
    /// <remarks>该方法没有分页保护，调用方应避免用于无界大集合。</remarks>
    Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>返回提供程序原生的可组合查询对象，供高级查询使用。</summary>
    /// <returns>尚未执行的实体查询。</returns>
    /// <remarks>EF Core 返回跟踪查询；MongoDB 返回 LINQ 查询。表达式翻译失败会在执行阶段抛出提供程序异常。</remarks>
    IQueryable<TEntity> Query();

    /// <summary>返回不需要变更跟踪的可组合查询对象。</summary>
    /// <returns>尚未执行的只读实体查询。</returns>
    /// <remarks>EF Core 使用无跟踪查询；MongoDB 本身没有 EF 式跟踪，因此与 <see cref="Query"/> 等价。</remarks>
    IQueryable<TEntity> QueryNoTracking();

    /// <summary>异步查询指定页，并返回筛选后的总数。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="pageIndex">从 1 开始的页码。</param>
    /// <param name="pageSize">每页最多返回的实体数，必须大于 0。</param>
    /// <param name="orderBy">可选排序表达式；为空时顺序由提供程序决定，不保证稳定。</param>
    /// <param name="isAsc">为 <see langword="true"/> 时升序，否则降序。</param>
    /// <param name="cancellationToken">用于取消计数和列表查询的令牌。</param>
    /// <returns>筛选后的总数与当前页实体序列。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> 或 <paramref name="pageSize"/> 小于 1 时抛出。</exception>
    /// <exception cref="OverflowException">分页偏移量或提供程序计数超出 <see cref="int"/> 范围时抛出。</exception>
    Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default);
}
