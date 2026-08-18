using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis.Connections;

/// <summary>
///     在缓存组件内部解析 Redis 数据库连接，不作为公共 API 暴露。
/// </summary>
/// <remarks>
///     实现按实例名称在进程内共享同一个 <see cref="IConnectionMultiplexer"/>，返回的
///     <see cref="IDatabase"/> 是轻量句柄，可以随取随用，不需要缓存或释放。
///     连接在首次解析时建立；若 Redis 不可达且连接字符串未关闭 <c>abortConnect</c>，异常会在此处抛出，
///     并由调用方的错误策略决定是否降级。
/// </remarks>
internal interface IRedisConnectionFactory
{
    /// <summary>
    ///     获取名称为 <c>default</c> 的实例上的 Redis 数据库句柄。
    /// </summary>
    /// <param name="database">Redis 逻辑库编号；<c>-1</c> 表示使用连接字符串中配置的默认库。</param>
    /// <returns>可直接执行命令的数据库句柄。</returns>
    /// <exception cref="InvalidOperationException">未配置默认连接字符串时抛出。</exception>
    /// <exception cref="ObjectDisposedException">工厂已释放时抛出。</exception>
    IDatabase GetDatabase(int database = -1);

    /// <summary>
    ///     获取指定命名实例上的 Redis 数据库句柄。
    /// </summary>
    /// <param name="instanceName">实例名称；空值或空白值按 <c>default</c> 处理。</param>
    /// <param name="database">Redis 逻辑库编号；<c>-1</c> 表示使用连接字符串中配置的默认库。</param>
    /// <returns>可直接执行命令的数据库句柄。</returns>
    /// <exception cref="InvalidOperationException">该实例没有配置连接字符串时抛出。</exception>
    /// <exception cref="ObjectDisposedException">工厂已释放时抛出。</exception>
    IDatabase GetDatabase(string instanceName, int database = -1);
}
