using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

/// <summary>
/// 仓储基类
/// </summary>
/// <param name="dbContext"></param>
/// <typeparam name="TDbContext"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public class Repository<TDbContext, TEntity>(TDbContext dbContext) : QueryRepository<TDbContext, TEntity>(dbContext), IRepository<TDbContext, TEntity>
    where TEntity : class, IAggregateRoot where TDbContext : DbContext
{
    public new TDbContext DbContext { get; set; } = dbContext;

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        return (await Entity.AddAsync(entity)).Entity;
    }

    public async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        await Entity.AddRangeAsync(entities);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        Entity.Update(entity);
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(TEntity entity)
    {
        Entity.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.Where(predicate).ExecuteDeleteAsync();
    }

    public Task<int> SaveChangesAsync()
    {
        return DbContext.SaveChangesAsync();
    }
}
