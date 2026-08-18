using System.Collections.Concurrent;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis.Connections;

/// <summary>
///     为每个命名实例惰性创建并复用 Redis 连接。
/// </summary>
/// <remarks>
///     每个实例名称对应一个 <see cref="Lazy{T}"/> 包装的 <see cref="IConnectionMultiplexer"/>，
///     使用 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> 保证并发解析时只建立一条连接。
///     连接通过同步的 <see cref="ConnectionMultiplexer.Connect(ConfigurationOptions, TextWriter)"/> 建立，
///     因此首次访问会阻塞当前线程直到连接成功或超时；本组件不做启动预热，也不创建连接池。
///     建立失败时缓存条目会被移除，异常向上抛出，下一次调用可以重新尝试。
/// </remarks>
internal sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private const string DefaultInstanceName = "default";

    private readonly ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>> _connections = new();
    private readonly object _lifecycleLock = new();
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly RedisConnectionOptions _options;
    private volatile bool _disposed;

    /// <summary>
    ///     初始化连接工厂，并立即校验是否配置了默认实例。
    /// </summary>
    /// <param name="options">Redis 连接选项。</param>
    /// <param name="logger">用于记录连接生命周期事件的日志记录器。</param>
    /// <exception cref="ArgumentNullException">任一参数为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">未配置默认 Redis 连接字符串时抛出。</exception>
    /// <remarks>
    ///     校验在构造时执行，因此即使业务调用只使用本地缓存，解析该服务仍要求存在默认连接字符串。
    /// </remarks>
    public RedisConnectionFactory(
        IOptions<RedisConnectionOptions> options,
        ILogger<RedisConnectionFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ValidateOptions();
    }

    /// <inheritdoc />
    public IDatabase GetDatabase(int database = -1)
    {
        return GetConnection(DefaultInstanceName).GetDatabase(database);
    }

    /// <inheritdoc />
    public IDatabase GetDatabase(string instanceName, int database = -1)
    {
        return GetConnection(instanceName).GetDatabase(database);
    }

    /// <summary>
    ///     释放所有已建立的 Redis 连接。
    /// </summary>
    /// <remarks>
    ///     释放后再次解析数据库会抛出 <see cref="ObjectDisposedException"/>。未实际建立值的惰性条目会被跳过，
    ///     单个连接释放失败只记录日志，不影响其余连接的释放。
    /// </remarks>
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

    /// <summary>
    ///     获取或惰性建立指定实例的共享连接。
    /// </summary>
    /// <remarks>与释放流程竞争时会清理刚建立的连接并抛出 <see cref="ObjectDisposedException"/>。</remarks>
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

    /// <summary>
    ///     创建实例对应的惰性连接包装，保证并发解析时只执行一次连接建立。
    /// </summary>
    private Lazy<IConnectionMultiplexer> CreateLazyConnection(string instanceName)
    {
        return new Lazy<IConnectionMultiplexer>(
            () => CreateConnection(instanceName),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    ///     同步建立连接并挂接连接失败、恢复和内部错误的日志订阅。
    /// </summary>
    /// <remarks>连接失败的异常在记录后原样抛出，不做静默降级。</remarks>
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

    /// <summary>
    ///     仅当字典中仍是同一个惰性包装时移除它，避免误删其他线程已重建的连接。
    /// </summary>
    private void RemoveIfCurrent(string instanceName, Lazy<IConnectionMultiplexer> connection)
    {
        if (_connections.TryGetValue(instanceName, out var current) && ReferenceEquals(current, connection))
            _connections.TryRemove(instanceName, out _);
    }

    /// <summary>
    ///     释放已实际建立的连接，释放异常只记录不抛出。
    /// </summary>
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

    /// <summary>
    ///     将空值或空白实例名归一化为 <c>default</c>。
    /// </summary>
    private static string NormalizeInstanceName(string? instanceName)
    {
        return string.IsNullOrWhiteSpace(instanceName) ? DefaultInstanceName : instanceName;
    }

    /// <summary>
    ///     校验默认实例是否具有连接字符串。
    /// </summary>
    /// <exception cref="InvalidOperationException">未配置默认 Redis 连接字符串时抛出。</exception>
    private void ValidateOptions()
    {
        if (!_options.HasInstance(DefaultInstanceName))
            throw new InvalidOperationException("未配置默认 Redis 连接字符串");
    }
}
