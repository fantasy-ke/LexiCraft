using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Factories;

/// <summary>
/// Redis 连接工厂接口
/// </summary>
public interface IRedisConnectionFactory
{
    /// <summary>
    /// 获取默认的 Redis 连接
    /// </summary>
    /// <returns>Redis 连接复用器</returns>
    IConnectionMultiplexer GetConnection();

    /// <summary>
    /// 根据实例名称获取 Redis 连接
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>Redis 连接复用器</returns>
    IConnectionMultiplexer GetConnection(string instanceName);

    /// <summary>
    /// 获取默认的 Redis 数据库
    /// </summary>
    /// <param name="database">数据库编号</param>
    /// <returns>Redis 数据库</returns>
    IDatabase GetDatabase(int database = -1);

    /// <summary>
    /// 根据实例名称获取 Redis 数据库
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="database">数据库编号</param>
    /// <returns>Redis 数据库</returns>
    IDatabase GetDatabase(string instanceName, int database = -1);

    /// <summary>
    /// 检查连接是否可用
    /// </summary>
    /// <param name="instanceName">实例名称，null 表示默认实例</param>
    /// <returns>连接是否可用</returns>
    bool IsConnected(string? instanceName = null);

    /// <summary>
    /// 获取连接信息
    /// </summary>
    /// <param name="instanceName">实例名称，null 表示默认实例</param>
    /// <returns>连接信息</returns>
    ConnectionInfo? GetConnectionInfo(string? instanceName = null);

    /// <summary>
    /// 获取所有连接信息
    /// </summary>
    /// <returns>所有连接信息</returns>
    IEnumerable<ConnectionInfo> GetAllConnectionInfo();

    /// <summary>
    /// 强制重新连接指定实例
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>是否重连成功</returns>
    Task<bool> ReconnectAsync(string? instanceName = null);
}