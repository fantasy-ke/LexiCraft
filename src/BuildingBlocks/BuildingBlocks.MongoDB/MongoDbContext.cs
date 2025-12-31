using BuildingBlocks.Abstractions;
using BuildingBlocks.MongoDB.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoDbContext : IMongoDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoClient Client { get; }
    public IClientSessionHandle? Session { get; private set; }

    public MongoDbContext(IMongoDatabase database, IMongoClient client)
    {
        Client = client;
        Database = database;
    }

    public async Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        Session = await Client.StartSessionAsync(cancellationToken: cancellationToken);
        Session.StartTransaction();
        return Session;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session != null && Session.IsInTransaction)
        {
            await Session.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session != null && Session.IsInTransaction)
        {
            await Session.AbortTransactionAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        Session?.Dispose();
        GC.SuppressFinalize(this);
    }
}
