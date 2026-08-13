using BuildingBlocks.Abstractions;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoDbContext(IMongoDatabase database, IMongoClient client) : IMongoDbContext
{
    public IMongoDatabase Database { get; } = database;
    public IMongoClient Client { get; } = client;
    public IClientSessionHandle? Session { get; private set; }

    public async Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is { IsInTransaction: true })
            throw new InvalidOperationException("A MongoDB transaction is already active for this context.");

        DisposeSession();
        var session = await Client.StartSessionAsync(cancellationToken: cancellationToken);

        try
        {
            session.StartTransaction();
            Session = session;
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is not { IsInTransaction: true } session) return;

        await session.CommitTransactionAsync(cancellationToken);
        DisposeSession();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is not { IsInTransaction: true } session) return;

        await session.AbortTransactionAsync(cancellationToken);
        DisposeSession();
    }

    public void Dispose()
    {
        DisposeSession();
        GC.SuppressFinalize(this);
    }

    private void DisposeSession()
    {
        Session?.Dispose();
        Session = null;
    }
}