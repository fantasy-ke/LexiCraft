using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.Domain;

/// <summary>
///     通用仓储接口
/// </summary>
/// <typeparam name="TDbContext">数据库上下文类型</typeparam>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    TDbContext DbContext { get; set; }
}

/// <summary>
///     通用仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    /// <summary>
    ///     插入实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>插入的实体</returns>
    Task<TEntity> InsertAsync(TEntity entity);

    /// <summary>
    ///     批量添加数据
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <returns></returns>
    Task InsertAsync(IEnumerable<TEntity> entities);

    /// <summary>
    ///     更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>更新的实体</returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    ///     删除实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns></returns>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    ///     删除符合条件的实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns></returns>
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     保存更改
    /// </summary>
    /// <returns>受影响的行数</returns>
    Task<int> SaveChangesAsync();
}