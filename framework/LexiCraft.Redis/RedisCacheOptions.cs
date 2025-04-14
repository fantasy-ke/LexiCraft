namespace LexiCraft.Redis;

public class RedisCacheOptions
{
    public bool Enable { get; set; }
    
    public string? ConnectionString { get; set; }
    
    public string? Password { get; set; }
    
    public int? DefaultDb { get; set; }
    
    public int? MaxPoolSize { get; set; }
    
    public int? MinPoolSize { get; set; }
    
    public bool? Ssl { get; set; }
    
    public string? KeyPrefix { get; set; }

    public string Configuration => GetOptionsConnectionString();

    public SideCaching SideCache { get; set; }

    /// <summary>
    /// 获取配置连接字符串
    /// </summary>
    /// <returns></returns>
    private string GetOptionsConnectionString()
    {
        var conString = "";
        if (!string.IsNullOrEmpty(ConnectionString))
            conString = ConnectionString;
        if (!string.IsNullOrEmpty(Password))
            conString += $",password={Password}";
        if (DefaultDb.HasValue)
            conString += $",database={DefaultDb.Value}";
        else
            conString += $",database=0";
        if (MaxPoolSize is > 0)
            conString += $",max pool size={MaxPoolSize.Value}";
        if (MinPoolSize is > 0)
            conString += $",min pool size={MinPoolSize.Value}";
        if (Ssl.HasValue)
            conString += $",ssl={Ssl.Value}";
        
        return conString;
    }
}

public class SideCaching
{
    public bool Enable { get; set; }
    /// <summary>
    /// 容量
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// 需要本地缓存的key
    /// </summary>
    public string KeyFilterCache { get; set; }

    /// <summary>
    /// 本地长期未使用的
    /// </summary>
    public int ExpiredMinutes { get; set; }
}
