using System.Linq.Expressions;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoRepository<TEntity>(IMongoDbContext context) : MongoQueryRepository<TEntity>(context), IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (context.Session != null && context.Session.IsInTransaction)
        {
            await Collection.InsertOneAsync(context.Session, entity);
        }
        else
        {
            await Collection.InsertOneAsync(entity);
        }

        return entity;
    }

    public async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        if (context.Session != null && context.Session.IsInTransaction)
        {
            await Collection.InsertManyAsync(context.Session, entities);
        }
        else
        {
            await Collection.InsertManyAsync(entities);
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (context.Session != null && context.Session.IsInTransaction)
        {
            await Collection.ReplaceOneAsync(context.Session, filter, entity);
        }
        else
        {
            await Collection.ReplaceOneAsync(filter, entity);
        }

        return entity;
    }

    public async Task DeleteAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (context.Session != null && context.Session.IsInTransaction)
        {
            await Collection.DeleteOneAsync(context.Session, filter);
        }
        else
        {
            await Collection.DeleteOneAsync(filter);
        }
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        if (context.Session is { IsInTransaction: true })
        {
            await Collection.DeleteManyAsync(context.Session, predicate);
        }
        else
        {
            await Collection.DeleteManyAsync(predicate);
        }
    }

    public Task<int> SaveChangesAsync()
    {
        return Task.FromResult(1);
    }
}
