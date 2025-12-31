using System.Linq.Expressions;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoRepository<TEntity>(IMongoDbContext context) : IRepository<TEntity>
    where TEntity : MongoEntity
{
    private readonly IMongoCollection<TEntity> _collection =
        context.Database.GetCollection<TEntity>(typeof(TEntity).Name);

    public IQueryable<TTemp> Select<TTemp>() where TTemp : class
    {
        throw new NotSupportedException(
            "MongoDB repository does not support arbitrary generic Select<T> directly like EF.");
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).ToListAsync();
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (context.Session != null && context.Session.IsInTransaction)
        {
            await _collection.InsertOneAsync(context.Session, entity);
        }
        else
        {
            await _collection.InsertOneAsync(entity);
        }

        return entity;
    }

    public async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        if (context.Session != null && context.Session.IsInTransaction)
        {
            await _collection.InsertManyAsync(context.Session, entities);
        }
        else
        {
            await _collection.InsertManyAsync(entities);
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (context.Session != null && context.Session.IsInTransaction)
        {
            await _collection.ReplaceOneAsync(context.Session, filter, entity);
        }
        else
        {
            await _collection.ReplaceOneAsync(filter, entity);
        }

        return entity;
    }

    public async Task DeleteAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (context.Session != null && context.Session.IsInTransaction)
        {
            await _collection.DeleteOneAsync(context.Session, filter);
        }
        else
        {
            await _collection.DeleteOneAsync(filter);
        }
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).FirstOrDefaultAsync()!;
    }

    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).FirstAsync();
    }

    public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).SingleOrDefaultAsync();
    }

    public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).SingleAsync();
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Task.FromResult((int)_collection.CountDocuments(predicate));
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _collection.Find(predicate).AnyAsync();
    }

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return FirstOrDefaultAsync(predicate);
    }

    public Task<List<TEntity>> GetListAsync()
    {
        return _collection.Find(_ => true).ToListAsync();
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        if (context.Session is { IsInTransaction: true })
        {
            await _collection.DeleteManyAsync(context.Session, predicate);
        }
        else
        {
            await _collection.DeleteManyAsync(predicate);
        }
    }

    public Task<int> SaveChangesAsync()
    {
        return Task.FromResult(1);
    }

    public IQueryable<T> QueryNoTracking<T>() where T : class
    {
        if (typeof(T) == typeof(TEntity))
        {
            return (IQueryable<T>)_collection.AsQueryable();
        }

        throw new NotSupportedException(
            "QueryNoTracking with different type not fully supported in this generic adapter.");
    }

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
    {
        var findOptions = _collection.Find(predicate);
        var total = (int)await findOptions.CountDocumentsAsync();

        if (orderBy != null)
        {
            findOptions = isAsc ? findOptions.SortBy(orderBy) : findOptions.SortByDescending(orderBy);
        }

        var result = await findOptions.Skip((pageIndex - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (total, result);
    }
}