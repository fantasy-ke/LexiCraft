namespace BuildingBlocks.Caching.Configuration;

/// <summary>
///     Redis 连接选项配置
/// </summary>
public class RedisConnectionOptions
{
    /// <summary>
    ///     默认连接字符串
    /// </summary>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    ///     命名实例连接字符串字典
    ///     键为实例名称，值为连接字符串
    /// </summary>
    public Dictionary<string, string> Instances { get; set; } = new();

    /// <summary>
    ///     连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    ///     同步操作超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    ///     异步操作超时时间（毫秒）
    /// </summary>
    public int AsyncTimeout { get; set; } = 5000;

    /// <summary>
    ///     连接重试次数
    /// </summary>
    public int ConnectRetry { get; set; } = 3;

    /// <summary>
    ///     连接失败时是否中止
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    ///     是否启用连接池
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    ///     最大连接池大小
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 10;

    /// <summary>
    ///     获取指定实例的连接字符串
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>连接字符串，如果不存在则返回默认连接字符串</returns>
    public string? GetConnectionString(string instanceName)
    {
        if (Instances.TryGetValue(instanceName, out var connectionString)) return connectionString;

        return instanceName == "default" ? DefaultConnectionString : null;
    }

    /// <summary>
    ///     添加或更新实例连接字符串
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="connectionString">连接字符串</param>
    public void SetInstance(string instanceName, string connectionString)
    {
        Instances[instanceName] = connectionString;
    }

    /// <summary>
    ///     检查是否配置了指定实例
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <returns>是否已配置</returns>
    public bool HasInstance(string instanceName)
    {
        return Instances.ContainsKey(instanceName) ||
               (instanceName == "default" && !string.IsNullOrEmpty(DefaultConnectionString));
    }
}