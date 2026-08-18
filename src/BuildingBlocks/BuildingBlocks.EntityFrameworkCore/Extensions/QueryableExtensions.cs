using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Extensions;

/// <summary>提供 EF Core 可查询对象的条件筛选、分页和无跟踪辅助方法。</summary>
public static class QueryableExtensions
{
    /// <summary>跳过指定元素并限制返回数量。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="skipCount">要跳过的元素数。</param>
    /// <param name="maxResultCount">最多返回的元素数。</param>
    /// <returns>组合了 <c>Skip</c> 和 <c>Take</c> 的查询。</returns>
    public static IQueryable<T> PageBy<T>(
        this IQueryable<T> query,
        int skipCount,
        int maxResultCount)
    {
        return query.Skip(skipCount).Take(maxResultCount);
    }

    /// <summary>分页并尽量保留调用方声明的查询类型。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <typeparam name="TQueryable">调用方声明的查询类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="skipCount">要跳过的元素数。</param>
    /// <param name="maxResultCount">最多返回的元素数。</param>
    /// <returns>分页后的查询。</returns>
    /// <exception cref="InvalidCastException">提供程序返回的查询对象不能转换为 <typeparamref name="TQueryable"/> 时抛出。</exception>
    public static TQueryable PageBy<T, TQueryable>(
        this TQueryable query,
        int skipCount,
        int maxResultCount)
        where TQueryable : IQueryable<T>
    {
        return (TQueryable)query.Skip(skipCount).Take(maxResultCount);
    }

    /// <summary>仅在条件成立时附加筛选表达式。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="condition">是否应用筛选。</param>
    /// <param name="predicate">条件成立时使用的筛选表达式。</param>
    /// <returns>原查询或附加筛选后的查询。</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return !condition ? query : query.Where(predicate);
    }

    /// <summary>仅在条件成立时附加筛选表达式，并尽量保留调用方声明的查询类型。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <typeparam name="TQueryable">调用方声明的查询类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="condition">是否应用筛选。</param>
    /// <param name="predicate">条件成立时使用的筛选表达式。</param>
    /// <returns>原查询或附加筛选后的查询。</returns>
    /// <exception cref="InvalidCastException">筛选后的查询对象不能转换为 <typeparamref name="TQueryable"/> 时抛出。</exception>
    public static TQueryable WhereIf<T, TQueryable>(
        this TQueryable query,
        bool condition,
        Expression<Func<T, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        return !condition ? query : (TQueryable)query.Where(predicate);
    }

    /// <summary>仅在条件成立时附加带元素索引的筛选表达式。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="condition">是否应用筛选。</param>
    /// <param name="predicate">接收元素及其索引的筛选表达式。</param>
    /// <returns>原查询或附加筛选后的查询。</returns>
    /// <remarks>数据库提供程序不一定支持翻译带索引的谓词，翻译能力应由调用方验证。</remarks>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, int, bool>> predicate)
    {
        return !condition ? query : query.Where(predicate);
    }

    /// <summary>仅在条件成立时附加带元素索引的筛选表达式，并尽量保留查询类型。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <typeparam name="TQueryable">调用方声明的查询类型。</typeparam>
    /// <param name="query">源查询。</param>
    /// <param name="condition">是否应用筛选。</param>
    /// <param name="predicate">接收元素及其索引的筛选表达式。</param>
    /// <returns>原查询或附加筛选后的查询。</returns>
    /// <exception cref="InvalidCastException">筛选后的查询对象不能转换为 <typeparamref name="TQueryable"/> 时抛出。</exception>
    public static TQueryable WhereIf<T, TQueryable>(
        this TQueryable query,
        bool condition,
        Expression<Func<T, int, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        return !condition ? query : (TQueryable)query.Where(predicate);
    }

    /// <summary>立即执行同步计数，并返回原查询以继续组合。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="queryable">要计数的查询。</param>
    /// <param name="count">查询的元素总数。</param>
    /// <returns>未改变的原查询。</returns>
    /// <remarks>该方法同步访问数据源；异步请求路径应优先使用提供程序的异步计数 API。</remarks>
    public static IQueryable<T> Count<T>(this IQueryable<T> queryable, out long count)
    {
        count = queryable.Count();
        return queryable;
    }

    /// <summary>把 EF Core 查询转换为无跟踪查询。</summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="queryable">源查询。</param>
    /// <returns>不会把查询结果加入 ChangeTracker 的查询。</returns>
    public static IQueryable<T> QueryNoTracking<T>(this IQueryable<T> queryable) where T : class
    {
        return queryable.AsNoTracking();
    }

    /// <summary>按从 1 开始的页码组合简单分页查询。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="queryable">源查询。</param>
    /// <param name="pageNumber">页码；小于 1 时按第 1 页处理。</param>
    /// <param name="pageSize">每页数量。</param>
    /// <returns>分页后的查询。</returns>
    /// <remarks>该辅助方法不校验页大小，也不添加排序；需要稳定分页时应先显式排序。</remarks>
    public static IQueryable<T> Page<T>(this IQueryable<T> queryable, int pageNumber, int pageSize)
    {
        queryable = queryable.Skip(Math.Max(0, pageNumber - 1) * pageSize).Take(pageSize);
        return queryable;
    }

    /// <summary>异步查询指定页，并返回筛选后的总数。</summary>
    /// <typeparam name="T">查询元素类型。</typeparam>
    /// <param name="queryable">源查询。</param>
    /// <param name="predicate">筛选表达式。</param>
    /// <param name="pageIndex">从 1 开始的页码。</param>
    /// <param name="pageSize">每页最多返回的元素数。</param>
    /// <param name="orderBy">可选排序表达式；为空时结果顺序不保证稳定。</param>
    /// <param name="isAsc">是否升序。</param>
    /// <param name="cancellationToken">用于取消计数和列表查询的令牌。</param>
    /// <returns>筛选后的总数与当前页结果。</returns>
    /// <exception cref="ArgumentOutOfRangeException">页码或页大小小于 1 时抛出。</exception>
    /// <exception cref="OverflowException">分页偏移量溢出时抛出。</exception>
    public static async Task<(int total, IEnumerable<T> result)> GetPageListAsync<T>(
        this IQueryable<T> queryable,
        Expression<Func<T, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var query = queryable.Where(predicate);
        var total = await query.CountAsync(cancellationToken);

        if (orderBy != null) query = isAsc ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        var skip = checked((pageIndex - 1) * pageSize);
        var list = await query.Skip(skip).Take(pageSize).ToArrayAsync(cancellationToken);

        return (total, list);
    }
}
