using System.Collections.Concurrent;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis.Connections;

/// <summary>
///     为每个命名实例惰性创建并复用 Redis 连接。
/// </summary>
internal sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private const string DefaultInstanceName = "default";

    private readonly ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>> _connections = new();
    private readonly object _lifecycleLock = new();
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly RedisConnectionOptions _options;
    private volatile bool _disposed;

    public RedisConnectionFactory(
        IOptions<RedisConnectionOptions> options,
        ILogger<RedisConnectionFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ValidateOptions();
    }

    public IDatabase GetDatabase(int database = -1)
    {
        return GetConnection(DefaultInstanceName).GetDatabase(database);
    }

    public IDatabase GetDatabase(string instanceName, int database = -1)
    {
        return GetConnection(instanceName).GetDatabase(database);
    }

    public void Dispose()
    {
        Lazy<IConnectionMultiplexer>[] connections;
        lock (_lifecycleLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            connections = _connections.Values.ToArray();
            _connections.Clear();
        }

        foreach (var connection in connections)
            DisposeConnection(connection);
    }

    private IConnectionMultiplexer GetConnection(string instanceName)
    {
        instanceName = NormalizeInstanceName(instanceName);

        Lazy<IConnectionMultiplexer> lazyConnection;
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            lazyConnection = _connections.GetOrAdd(instanceName, CreateLazyConnection);
        }

        try
        {
            var connection = lazyConnection.Value;
            if (!_disposed)
                return connection;

            RemoveIfCurrent(instanceName, lazyConnection);
            DisposeConnection(lazyConnection);
            throw new ObjectDisposedException(nameof(RedisConnectionFactory));
        }
        catch
        {
            RemoveIfCurrent(instanceName, lazyConnection);
            throw;
        }
    }

    private Lazy<IConnectionMultiplexer> CreateLazyConnection(string instanceName)
    {
        return new Lazy<IConnectionMultiplexer>(
            () => CreateConnection(instanceName),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private IConnectionMultiplexer CreateConnection(string instanceName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _logger.LogInformation("正在创建 Redis 连接: {InstanceName}", instanceName);

        try
        {
            var connection = ConnectionMultiplexer.Connect(_options.CreateConfigurationOptions(instanceName));
            connection.ConnectionFailed += (_, args) =>
                _logger.LogError(
                    "Redis 内部错误: {InstanceName}, 端点: {EndPoint}, 异常: {Exception}",
                    instanceName,
                    args.EndPoint,
                    args.Exception?.Message);
            connection.ConnectionRestored += (_, args) =>
                _logger.LogInformation(
                    "Redis 连接已恢复: {InstanceName}, 端点: {EndPoint}",
                    instanceName,
                    args.EndPoint);
            connection.InternalError += (_, args) =>
                _logger.LogError(
                    "Redis 内部错误: {InstanceName}, 端点: {EndPoint}, 异常: {Exception}",
                    instanceName,
                    args.EndPoint,
                    args.Exception?.Message);

            _logger.LogInformation("Redis 连接已建立: {InstanceName}", instanceName);
            return connection;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "创建 Redis 连接失败: {InstanceName}", instanceName);
            throw;
        }
    }

    private void RemoveIfCurrent(string instanceName, Lazy<IConnectionMultiplexer> connection)
    {
        if (_connections.TryGetValue(instanceName, out var current) && ReferenceEquals(current, connection))
            _connections.TryRemove(instanceName, out _);
    }

    private void DisposeConnection(Lazy<IConnectionMultiplexer> connection)
    {
        if (!connection.IsValueCreated)
            return;

        try
        {
            connection.Value.Dispose();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "释放 Redis 连接失败");
        }
    }

    private static string NormalizeInstanceName(string? instanceName)
    {
        return string.IsNullOrWhiteSpace(instanceName) ? DefaultInstanceName : instanceName;
    }

    private void ValidateOptions()
    {
        if (!_options.HasInstance(DefaultInstanceName))
            throw new InvalidOperationException("未配置默认 Redis 连接字符串");
    }
}
