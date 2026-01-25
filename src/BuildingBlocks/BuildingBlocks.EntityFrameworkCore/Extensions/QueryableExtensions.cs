using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> PageBy<T>(
        this IQueryable<T> query,
        int skipCount,
        int maxResultCount
    )
    {
        return query.Skip(skipCount).Take(maxResultCount);
    }

    public static TQueryable PageBy<T, TQueryable>(
        this TQueryable query,
        int skipCount,
        int maxResultCount
    )
        where TQueryable : IQueryable<T>
    {
        return (TQueryable)query.Skip(skipCount).Take(maxResultCount);
    }

    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate
    )
    {
        return !condition ? query : query.Where(predicate);
    }

    public static TQueryable WhereIf<T, TQueryable>(
        this TQueryable query,
        bool condition,
        Expression<Func<T, bool>> predicate
    )
        where TQueryable : IQueryable<T>
    {
        return !condition ? query : (TQueryable)query.Where(predicate);
    }

    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, int, bool>> predicate
    )
    {
        return !condition ? query : query.Where(predicate);
    }

    public static TQueryable WhereIf<T, TQueryable>(
        this TQueryable query,
        bool condition,
        Expression<Func<T, int, bool>> predicate
    )
        where TQueryable : IQueryable<T>
    {
        return !condition ? query : (TQueryable)query.Where(predicate);
    }

    public static IQueryable<T> Count<T>(this IQueryable<T> queryable, out long count)
    {
        count = queryable.Count();
        return queryable;
    }

    public static IQueryable<T> QueryNoTracking<T>(this IQueryable<T> queryable) where T : class
    {
        return queryable.AsNoTracking();
    }

    public static IQueryable<T> Page<T>(this IQueryable<T> queryable, int pageNumber, int pageSize)
    {
        queryable = queryable.Skip(Math.Max(0, pageNumber - 1) * pageSize).Take(pageSize);
        return queryable;
    }

    public static async Task<(int total, IEnumerable<T> result)> GetPageListAsync<T>(
        this IQueryable<T> queryable,
        Expression<Func<T, bool>> predicate, int pageIndex,
        int pageSize, Expression<Func<T, object>>? orderBy = null, bool isAsc = true)
    {
        var query = queryable.Where(predicate);

        var total = await query.CountAsync();

        if (orderBy != null) query = isAsc ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        var list = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();

        return (total, list);
    }
}