using System.Linq.Expressions;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

/// <summary>
/// 仓储基类
/// </summary>
/// <param name="dbContext"></param>
/// <typeparam name="TDbContext"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public class Repository<TDbContext, TEntity>(TDbContext dbContext) : IRepository<TDbContext, TEntity>
	where TEntity : class where TDbContext : DbContext
{
	
	public TDbContext DbContext { get; set; } = dbContext;
	
	private DbSet<TEntity> Entity => DbContext.Set<TEntity>();

	public IQueryable<TTemp> Select<TTemp>() where TTemp : class
	{
		return DbContext.Set<TTemp>();
	}
	
	public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.Where(predicate).ToListAsync();
	}

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

	public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return await Entity.FirstOrDefaultAsync(predicate).ConfigureAwait(false);
	}

	public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.FirstAsync(predicate);
	}

	public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.SingleOrDefaultAsync(predicate)!;
	}

	public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.SingleAsync(predicate);
	}

	public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.CountAsync(predicate);
	}

	public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.AnyAsync(predicate);
	}

	public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.FirstOrDefaultAsync(predicate);
	}

	public Task<List<TEntity>> GetListAsync()
	{
		return Entity.ToListAsync();
	}

	public Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
	{
		return Entity.Where(predicate).ExecuteDeleteAsync();
	}

	public Task<int> SaveChangesAsync()
	{
		return DbContext.SaveChangesAsync();
	}

	/// <summary>
	/// 查询无跟踪
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public IQueryable<TEntity> QueryNoTracking()
	{
		return DbContext.Set<TEntity>().AsNoTracking();
	}
	
	/// <summary>
	/// 查询无跟踪
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public IQueryable<T> QueryNoTracking<T>() where T : class
	{
		return DbContext.Set<T>().AsNoTracking();
	}
	
	public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
		Expression<Func<TEntity, bool>> predicate, int pageIndex,
		int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
	{
		var query = Entity.Where(predicate);

		var total = await query.CountAsync();

		if (orderBy != null)
		{
			query = isAsc ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
		}

		var list = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();

		return (total, list);
	}
}