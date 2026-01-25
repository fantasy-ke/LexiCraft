using System.Linq.Expressions;

namespace BuildingBlocks.Domain;

/// <summary>
///     只读仓储接口
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IQueryRepository<TEntity>
{
    IQueryable<TTemp> Select<TTemp>() where TTemp : class;

    /// <summary>
    ///     获取符合条件的实体列表
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体列表</returns>
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的第一个实体或默认值
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体对象或默认值</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的第一个实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体对象</returns>
    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的唯一实体或默认值
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体对象或默认值</returns>
    Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的唯一实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体对象</returns>
    Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的实体数量
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体数量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     判断是否存在符合条件的实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>是否存在</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取符合条件的实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体对象</returns>
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取所有实体列表
    /// </summary>
    /// <returns>实体列表</returns>
    Task<List<TEntity>> GetListAsync();

    /// <summary>
    ///     非跟踪查询
    /// </summary>
    /// <returns></returns>
    IQueryable<TEntity> Query();

    IQueryable<TEntity> QueryNoTracking();

    /// <summary>
    ///     非跟踪查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IQueryable<T> QueryNoTracking<T>() where T : class;

    /// <summary>
    ///     获取分页列表
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序字段</param>
    /// <param name="isAsc">是否升序</param>
    /// <returns>分页结果</returns>
    Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true);
}