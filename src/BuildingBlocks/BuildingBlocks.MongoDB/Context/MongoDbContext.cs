using BuildingBlocks.MongoDB.Abstractions;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Context;

/// <summary>管理 MongoDB 数据库访问和单个事务 session 生命周期。</summary>
/// <param name="database">当前应用使用的数据库。</param>
/// <param name="client">用于创建 session 的共享客户端。</param>
/// <remarks>提交或回滚成功后释放 session；操作失败时保留 session，供调用方完成事务收尾。</remarks>
public class MongoDbContext(IMongoDatabase database, IMongoClient client) : IMongoDbContext
{
    /// <inheritdoc />
    public IMongoDatabase Database { get; } = database;

    /// <inheritdoc />
    public IMongoClient Client { get; } = client;

    /// <inheritdoc />
    public IClientSessionHandle? Session { get; private set; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is not { IsInTransaction: true } session) return;

        await session.CommitTransactionAsync(cancellationToken);
        DisposeSession();
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Session is not { IsInTransaction: true } session) return;

        await session.AbortTransactionAsync(cancellationToken);
        DisposeSession();
    }

    /// <summary>释放当前 session，并抑制终结。</summary>
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
