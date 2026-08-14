using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Context;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.MongoDB.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using MongoDB.Driver;

namespace BuildingBlocks.Persistence.Tests;

public class MongoRepositoryTests
{
    [Fact]
    public async Task Empty_batch_insert_is_a_no_op_without_contacting_the_server()
    {
        var client = CreateOfflineClient();
        using var context = new MongoDbContext(client.GetDatabase("test_database"), client);
        var resilienceService = new CountingResilienceService();
        var repository = new MongoRepository<TestAggregate>(
            context,
            resilienceService,
            new NoOpPerformanceMonitor());

        await repository.InsertAsync(Array.Empty<TestAggregate>());

        Assert.Equal(0, resilienceService.ExecutionCount);
    }

    [Fact]
    public async Task Read_pipeline_uses_resilience_outside_a_transaction()
    {
        var client = CreateOfflineClient();
        using var context = new StubMongoDbContext(client.GetDatabase("test_database"), client);
        var resilienceService = new CountingResilienceService();
        var repository = new TestMongoQueryRepository(
            context,
            resilienceService,
            new NoOpPerformanceMonitor());

        var result = await repository.ExecuteReadPipelineAsync(session => Task.FromResult(session is null));

        Assert.True(result);
        Assert.Equal(1, resilienceService.ExecutionCount);
    }

    [Fact]
    public async Task Read_pipeline_bypasses_individual_retries_inside_a_transaction()
    {
        var client = CreateOfflineClient();
        var session = DispatchProxy.Create<IClientSessionHandle, ActiveSessionProxy>();
        using var context = new StubMongoDbContext(client.GetDatabase("test_database"), client, session);
        var resilienceService = new CountingResilienceService();
        var repository = new TestMongoQueryRepository(
            context,
            resilienceService,
            new NoOpPerformanceMonitor());
        IClientSessionHandle? observedSession = null;

        var result = await repository.ExecuteReadPipelineAsync(currentSession =>
        {
            observedSession = currentSession;
            return Task.FromResult(42);
        });

        Assert.Equal(42, result);
        Assert.Same(session, observedSession);
        Assert.Equal(0, resilienceService.ExecutionCount);
    }

    [Fact]
    public void Explicit_collection_name_is_used_by_the_repository_and_driver_collection()
    {
        var client = CreateOfflineClient();
        using var context = new StubMongoDbContext(client.GetDatabase("test_database"), client);
        var repository = new TestMongoQueryRepository(
            context,
            new CountingResilienceService(),
            new NoOpPerformanceMonitor(),
            "practice_tasks");

        Assert.Equal("practice_tasks", repository.CurrentCollectionName);
        Assert.Equal("practice_tasks", repository.DriverCollectionName);
    }

    [Fact]
    public async Task Write_pipeline_bypasses_application_retry_and_uses_the_active_session()
    {
        var client = CreateOfflineClient();
        var session = DispatchProxy.Create<IClientSessionHandle, ActiveSessionProxy>();
        using var context = new StubMongoDbContext(client.GetDatabase("test_database"), client, session);
        var resilienceService = new CountingResilienceService();
        var repository = new TestMongoQueryRepository(
            context,
            resilienceService,
            new NoOpPerformanceMonitor());
        IClientSessionHandle? observedSession = null;

        var result = await repository.ExecuteWritePipelineAsync(currentSession =>
        {
            observedSession = currentSession;
            return Task.FromResult(42);
        });

        Assert.Equal(42, result);
        Assert.Same(session, observedSession);
        Assert.Equal(0, resilienceService.ExecutionCount);
    }

    private static MongoClient CreateOfflineClient()
    {
        return new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost", 1),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(10)
        });
    }

    private sealed class TestAggregate : MongoEntity, IAggregateRoot
    {
    }

    private sealed class TestMongoQueryRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        string? collectionName = null)
        : MongoQueryRepository<TestAggregate>(context, resilienceService, performanceMonitor, collectionName)
    {
        public string CurrentCollectionName => CollectionName;
        public string DriverCollectionName => Collection.CollectionNamespace.CollectionName;

        public Task<TResult> ExecuteReadPipelineAsync<TResult>(
            Func<IClientSessionHandle?, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return ExecuteReadOperationAsync("TestRead", operation, cancellationToken);
        }

        public Task<TResult> ExecuteWritePipelineAsync<TResult>(
            Func<IClientSessionHandle?, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return ExecuteWriteOperationAsync("TestWrite", operation, cancellationToken);
        }
    }

    private sealed class StubMongoDbContext(
        IMongoDatabase database,
        IMongoClient client,
        IClientSessionHandle? session = null) : IMongoDbContext
    {
        public IMongoDatabase Database { get; } = database;
        public IMongoClient Client { get; } = client;
        public IClientSessionHandle? Session { get; } = session;

        public Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private class ActiveSessionProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_IsInTransaction") return true;
            if (targetMethod?.ReturnType == typeof(void)) return null;
            return targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class CountingResilienceService : IMongoResilienceService
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return await operation();
        }

        public async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            await operation();
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class NoOpPerformanceMonitor : IMongoPerformanceMonitor
    {
        public IDisposable StartOperation(string operationName, string collectionName)
        {
            return NoOpDisposable.Instance;
        }

        public Task<PerformanceMetrics> GetMetricsAsync(TimeSpan? period = null)
        {
            return Task.FromResult(new PerformanceMetrics());
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}