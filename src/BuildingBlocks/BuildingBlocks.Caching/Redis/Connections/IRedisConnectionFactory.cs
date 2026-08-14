using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis.Connections;

/// <summary>
///     在缓存组件内部解析 Redis 数据库连接，不作为公共 API 暴露。
/// </summary>
internal interface IRedisConnectionFactory
{
    IDatabase GetDatabase(int database = -1);

    IDatabase GetDatabase(string instanceName, int database = -1);
}
