using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Caching.Redis;

public class RedisCacheOptions
{
    public bool Enable { get; set; } = true;
    
    public string? ConnectionString { get; set; }
    
    public string? Password { get; set; }
    
    public int? DefaultDb { get; set; }
    
    public int? MaxPoolSize { get; set; }
    
    public int? MinPoolSize { get; set; }
    
    public bool? Ssl { get; set; }
    
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int? ConnectionTimeout { get; set; }

    /// <summary>
    /// 读取/写入超时时间（毫秒）
    /// </summary>
    public int? SyncTimeout { get; set; }

    public string Configuration => GetOptionsConnectionString();

    /// <summary>
    /// 本地缓存（一级缓存）配置
    /// </summary>
    public SideCaching SideCache { get; set; } = new();

    private string GetOptionsConnectionString()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return string.Empty;

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = ConnectionString.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2) parameters[kv[0].Trim()] = kv[1].Trim();
            else parameters[kv[0].Trim()] = string.Empty;
        }

        // Apply options from properties (override if already in ConnectionString)
        if (!string.IsNullOrEmpty(Password)) parameters["password"] = Password;
        if (DefaultDb.HasValue) parameters["database"] = DefaultDb.Value.ToString();
        else if (!parameters.ContainsKey("database")) parameters["database"] = "0";

        if (MaxPoolSize > 0) parameters["max pool size"] = MaxPoolSize.Value.ToString();
        if (MinPoolSize > 0) parameters["min pool size"] = MinPoolSize.Value.ToString();
        if (Ssl.HasValue) parameters["ssl"] = Ssl.Value.ToString().ToLower();
        if (ConnectionTimeout.HasValue) parameters["connectTimeout"] = ConnectionTimeout.Value.ToString();
        if (SyncTimeout.HasValue) parameters["syncTimeout"] = SyncTimeout.Value.ToString();

        return string.Join(",", parameters.Select(kv => string.IsNullOrEmpty(kv.Value) ? kv.Key : $"{kv.Key}={kv.Value}"));
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
