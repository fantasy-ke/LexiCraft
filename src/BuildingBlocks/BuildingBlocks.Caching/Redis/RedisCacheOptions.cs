using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Caching.Redis;

public class RedisCacheOptions
{
    public bool Enable { get; set; } = true;
    
    public string Host { get; set; } = "127.0.0.1";
    
    public int Port { get; set; } = 6379;

    public string? User { get; set; }
    
    public string? Password { get; set; }
    
    public int DefaultDb { get; set; } = 0;
    
    public int MaxPoolSize { get; set; } = 100;
    
    public int MinPoolSize { get; set; } = 1;

    public int IdleTimeout { get; set; } = 20000;
    
    public bool Ssl { get; set; } = false;
    
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 5000;

    /// <summary>
    /// 读取/写入超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// 本地缓存（一级缓存）配置
    /// </summary>
    public SideCaching SideCache { get; set; } = new();

    public string GetConnectionString()
    {
        return new FreeRedis.ConnectionStringBuilder
        {
            Host = $"{Host}:{Port}",
            User = User,
            Password = Password,
            Database = DefaultDb,
            MaxPoolSize = MaxPoolSize,
            MinPoolSize = MinPoolSize,
            Ssl = Ssl,
            ConnectTimeout = TimeSpan.FromMilliseconds(ConnectionTimeout),
            ReceiveTimeout = TimeSpan.FromMilliseconds(SyncTimeout),
            SendTimeout = TimeSpan.FromMilliseconds(SyncTimeout),
            IdleTimeout = TimeSpan.FromMilliseconds(IdleTimeout),
            // Name property does not exist in FreeRedis.ConnectionStringBuilder
        }.ToString();
    }
}

public class SideCaching
{
    /// <summary>
    /// 是否开启本地二级缓存 (FreeRedis ClientSideCaching)
    /// </summary>
    public bool Enable { get; set; }
    
    /// <summary>
    /// 本地缓存容量
    /// </summary>
    public int Capacity { get; set; } = 10000;

    /// <summary>
    /// 过滤哪些键能被本地缓存（前缀）
    /// </summary>
    public string? KeyFilterCache { get; set; }

    /// <summary>
    /// 本地长期未使用过期时间（分钟）
    /// </summary>
    public int ExpiredMinutes { get; set; } = 30;
}
