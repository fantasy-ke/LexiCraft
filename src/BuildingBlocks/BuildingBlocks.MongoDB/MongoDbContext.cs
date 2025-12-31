using BuildingBlocks.Abstractions;
using BuildingBlocks.MongoDB.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoDbContext(IMongoDatabase database, IMongoClient client) : IMongoDbContext
{
    public IMongoDatabase Database { get; } = database;
    public IMongoClient Client { get; } = client;
    public IClientSessionHandle? Session { get; private set; }

    public async Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        Session = await Client.StartSessionAsync(cancellationToken: cancellationToken);
        Session.StartTransaction();
        return Session;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is { IsInTransaction: true })
        {
            await Session.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is { IsInTransaction: true })
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
