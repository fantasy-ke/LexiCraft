using BuildingBlocks.Caching.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Factories;

/// <summary>
/// Redis 连接工厂实现
/// </summary>
public class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly RedisConnectionOptions _options;
    private readonly ConcurrentDictionary<string, IConnectionMultiplexer> _connections;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 默认实例名称
    /// </summary>
    public const string DefaultInstanceName = "default";

    /// <summary>
    /// 初始化 Redis 连接工厂
    /// </summary>
    /// <param name="options">Redis 连接选项</param>
    /// <param name="logger">日志记录器</param>
    public RedisConnectionFactory(
        IOptions<RedisConnectionOptions> options,
        ILogger<RedisConnectionFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connections = new ConcurrentDictionary<string, IConnectionMultiplexer>();

        ValidateOptions();
    }

    /// <summary>
    /// 获取默认的 Redis 连接
    /// </summary>
    /// <returns>Redis 连接复用器</returns>
    public IConnectionMultiplexer GetConnection()
    {
        return GetConnection(DefaultInstanceName);
    }

    /// <summary>
    /// 根据实例名称获取 Redis 连接
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>Redis 连接复用器</returns>
    public IConnectionMultiplexer GetConnection(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            instanceName = DefaultInstanceName;

        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisConnectionFactory));

        return _connections.GetOrAdd(instanceName, CreateConnection);
    }

    /// <summary>
    /// 获取默认的 Redis 数据库
    /// </summary>
    /// <param name="database">数据库编号</param>
    /// <returns>Redis 数据库</returns>
    public IDatabase GetDatabase(int database = -1)
    {
        return GetConnection().GetDatabase(database);
    }

    /// <summary>
    /// 根据实例名称获取 Redis 数据库
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="database">数据库编号</param>
    /// <returns>Redis 数据库</returns>
    public IDatabase GetDatabase(string instanceName, int database = -1)
    {
        return GetConnection(instanceName).GetDatabase(database);
    }

    /// <summary>
    /// 检查连接是否可用
    /// </summary>
    /// <param name="instanceName">实例名称，null 表示默认实例</param>
    /// <returns>连接是否可用</returns>
    public bool IsConnected(string? instanceName = null)
    {
        instanceName ??= DefaultInstanceName;

        if (_disposed || !_connections.TryGetValue(instanceName, out var connection))
            return false;

        return connection.IsConnected;
    }

    /// <summary>
    /// 获取连接信息
    /// </summary>
    /// <param name="instanceName">实例名称，null 表示默认实例</param>
    /// <returns>连接信息</returns>
    public ConnectionInfo? GetConnectionInfo(string? instanceName = null)
    {
        instanceName ??= DefaultInstanceName;

        if (_disposed || !_connections.TryGetValue(instanceName, out var connection))
            return null;

        return new ConnectionInfo
        {
            InstanceName = instanceName,
            IsConnected = connection.IsConnected,
            ClientName = connection.ClientName,
            ConfigurationString = connection.Configuration,
            TimeoutMilliseconds = connection.TimeoutMilliseconds
        };
    }

    /// <summary>
    /// 获取所有连接信息
    /// </summary>
    /// <returns>所有连接信息</returns>
    public IEnumerable<ConnectionInfo> GetAllConnectionInfo()
    {
        if (_disposed)
            return Enumerable.Empty<ConnectionInfo>();

        return _connections.Select(kvp => new ConnectionInfo
        {
            InstanceName = kvp.Key,
            IsConnected = kvp.Value.IsConnected,
            ClientName = kvp.Value.ClientName,
            ConfigurationString = kvp.Value.Configuration,
            TimeoutMilliseconds = kvp.Value.TimeoutMilliseconds
        }).ToList();
    }

    /// <summary>
    /// 强制重新连接指定实例
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>是否重连成功</returns>
    public async Task<bool> ReconnectAsync(string? instanceName = null)
    {
        instanceName ??= DefaultInstanceName;

        if (_disposed)
            return false;

        try
        {
            // 移除现有连接
            if (_connections.TryRemove(instanceName, out var oldConnection))
            {
                oldConnection.Dispose();
            }

            // 创建新连接
            var newConnection = CreateConnection(instanceName);
            _connections.TryAdd(instanceName, newConnection);

            _logger.LogInformation("Redis 实例 {InstanceName} 重连成功", instanceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 实例 {InstanceName} 重连失败", instanceName);
            return false;
        }
    }

    /// <summary>
    /// 创建 Redis 连接
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>Redis 连接复用器</returns>
    private IConnectionMultiplexer CreateConnection(string instanceName)
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisConnectionFactory));

            var connectionString = _options.GetConnectionString(instanceName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Redis 实例 '{instanceName}' 未配置连接字符串");
            }

            _logger.LogInformation("正在创建 Redis 连接: {InstanceName}", instanceName);

            try
            {
                var configurationOptions = ConfigurationOptions.Parse(connectionString);
                
                // 应用配置选项
                configurationOptions.AbortOnConnectFail = _options.AbortOnConnectFail;
                configurationOptions.ConnectRetry = _options.ConnectRetry;
                configurationOptions.ConnectTimeout = _options.ConnectTimeout;
                configurationOptions.SyncTimeout = _options.SyncTimeout;
                configurationOptions.AsyncTimeout = _options.AsyncTimeout;

                var connection = ConnectionMultiplexer.Connect(configurationOptions);

                // 注册连接事件
                connection.ConnectionFailed += (sender, args) =>
                {
                    _logger.LogError("Redis 连接失败: {InstanceName}, 端点: {EndPoint}, 异常: {Exception}",
                        instanceName, args.EndPoint, args.Exception?.Message);
                };

                connection.ConnectionRestored += (sender, args) =>
                {
                    _logger.LogInformation("Redis 连接已恢复: {InstanceName}, 端点: {EndPoint}",
                        instanceName, args.EndPoint);
                };

                connection.InternalError += (sender, args) =>
                {
                    _logger.LogError("Redis 内部错误: {InstanceName}, 端点: {EndPoint}, 异常: {Exception}",
                        instanceName, args.EndPoint, args.Exception?.Message);
                };

                _logger.LogInformation("Redis 连接创建成功: {InstanceName}", instanceName);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建 Redis 连接失败: {InstanceName}", instanceName);
                throw;
            }
        }
    }

    /// <summary>
    /// 验证配置选项
    /// </summary>
    private void ValidateOptions()
    {
        if (!_options.HasInstance(DefaultInstanceName))
        {
            throw new InvalidOperationException("必须配置默认 Redis 连接字符串或默认实例");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var connection in _connections.Values)
        {
            try
            {
                connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放 Redis 连接时发生异常");
            }
        }

        _connections.Clear();
    }
}

/// <summary>
/// Redis 连接信息
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// 实例名称
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 客户端名称
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// 配置选项字符串
    /// </summary>
    public string? ConfigurationString { get; set; }

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMilliseconds { get; set; }
}