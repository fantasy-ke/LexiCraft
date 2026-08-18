using StackExchange.Redis;

namespace BuildingBlocks.Caching.Options;

/// <summary>
///     定义默认及命名 Redis 实例的连接字符串和 StackExchange.Redis 连接参数。
/// </summary>
/// <remarks>
///     每个实例名称在进程内惰性创建并共享一个 <see cref="IConnectionMultiplexer"/>；本组件不创建连接池。
///     连接字符串内显式提供的同名参数优先于此类型的全局默认值。
/// </remarks>
public class RedisConnectionOptions
{
    /// <summary>
    ///     获取或设置名称为 <c>default</c> 的 Redis 连接字符串。
    /// </summary>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    ///     获取或设置命名实例到连接字符串的映射；实例名称按字典默认比较规则匹配。
    /// </summary>
    public Dictionary<string, string> Instances { get; set; } = new();

    /// <summary>
    ///     获取或设置建立连接的超时毫秒数；默认 5000，仅在连接字符串未指定 <c>connectTimeout</c> 时使用。
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    ///     获取或设置同步操作的超时毫秒数；默认 5000，仅在连接字符串未指定 <c>syncTimeout</c> 时使用。
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    ///     获取或设置异步操作的超时毫秒数；默认 5000，仅在连接字符串未指定 <c>asyncTimeout</c> 时使用。
    /// </summary>
    public int AsyncTimeout { get; set; } = 5000;

    /// <summary>
    ///     获取或设置初始连接重试次数；默认 3，仅在连接字符串未指定 <c>connectRetry</c> 时使用。
    /// </summary>
    public int ConnectRetry { get; set; } = 3;

    /// <summary>
    ///     获取或设置初始连接失败时是否中止；默认 <see langword="false"/>，仅在连接字符串未指定 <c>abortConnect</c> 时使用。
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    ///     获取或设置旧版连接池兼容值。
    /// </summary>
    /// <remarks>当前实现始终按命名实例共享单个连接，此值不参与运行时行为。</remarks>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    ///     获取或设置旧版最大连接池大小兼容值。
    /// </summary>
    /// <remarks>当前实现不创建连接池，此值不参与运行时行为。</remarks>
    public int MaxConnectionPoolSize { get; set; } = 10;

    /// <summary>
    ///     获取指定实例的原始连接字符串。
    /// </summary>
    /// <param name="instanceName">实例名称；只有精确的 <c>default</c> 会回退到 <see cref="DefaultConnectionString"/>。</param>
    /// <returns>命名实例连接字符串、默认连接字符串，或实例未配置时的 <see langword="null"/>。</returns>
    public string? GetConnectionString(string instanceName)
    {
        if (Instances.TryGetValue(instanceName, out var connectionString)) return connectionString;

        return instanceName == "default" ? DefaultConnectionString : null;
    }

    /// <summary>
    ///     为指定实例解析 StackExchange.Redis 配置，并补入连接字符串中未显式指定的全局连接参数。
    /// </summary>
    /// <param name="instanceName">要解析的实例名称。</param>
    /// <returns>可用于创建共享连接的 Redis 配置。</returns>
    /// <exception cref="InvalidOperationException">实例没有可用连接字符串时抛出。</exception>
    /// <exception cref="ArgumentException">连接字符串格式无效时由 StackExchange.Redis 抛出。</exception>
    public ConfigurationOptions CreateConfigurationOptions(string instanceName)
    {
        var connectionString = GetConnectionString(instanceName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Redis instance '{instanceName}' has no connection string");

        var configuration = ConfigurationOptions.Parse(connectionString);
        if (!HasConfigurationOption(connectionString, "abortConnect"))
            configuration.AbortOnConnectFail = AbortOnConnectFail;
        if (!HasConfigurationOption(connectionString, "connectRetry"))
            configuration.ConnectRetry = ConnectRetry;
        if (!HasConfigurationOption(connectionString, "connectTimeout"))
            configuration.ConnectTimeout = ConnectTimeout;
        if (!HasConfigurationOption(connectionString, "syncTimeout"))
            configuration.SyncTimeout = SyncTimeout;
        if (!HasConfigurationOption(connectionString, "asyncTimeout"))
            configuration.AsyncTimeout = AsyncTimeout;

        return configuration;
    }

    private static bool HasConfigurationOption(string connectionString, string optionName)
    {
        return connectionString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part =>
            {
                var separatorIndex = part.IndexOf('=');
                return separatorIndex > 0 &&
                       string.Equals(part[..separatorIndex].Trim(), optionName, StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>
    ///     添加或替换命名实例的连接字符串。
    /// </summary>
    /// <param name="instanceName">实例名称。</param>
    /// <param name="connectionString">Redis 连接字符串；敏感凭据应由外部机密配置提供。</param>
    public void SetInstance(string instanceName, string connectionString)
    {
        Instances[instanceName] = connectionString;
    }

    /// <summary>
    ///     检查指定命名实例，或名称为 <c>default</c> 的默认实例，是否具有连接字符串。
    /// </summary>
    /// <param name="instanceName">实例名称。</param>
    /// <returns>实例已配置时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public bool HasInstance(string instanceName)
    {
        return Instances.ContainsKey(instanceName) ||
               (instanceName == "default" && !string.IsNullOrEmpty(DefaultConnectionString));
    }
}