using MongoDB.Driver;

namespace BuildingBlocks.Abstractions;

public interface IMongoDbContext : IDisposable
{
    IMongoDatabase Database { get; }
    IMongoClient Client { get; }
    IClientSessionHandle? Session { get; }

    Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
