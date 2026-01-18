# 缓存服务配置示例

## appsettings.json 配置示例

### 基本配置

```json
{
  "Redis": {
    "DefaultConnectionString": "localhost:6379",
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AsyncTimeout": 5000,
    "ConnectRetry": 3,
    "AbortOnConnectFail": false
  },
  "CacheService": {
    "UseDistributed": true,
    "UseLocal": false,
    "Expiry": "01:00:00",
    "HideErrors": true,
    "EnableCompression": false,
    "EnableBinarySerialization": false,
    "EnableLock": true,
    "LockTimeout": "00:00:01",
    "LockAcquireTimeout": "00:00:01",
    "FallbackToFactory": true
  }
}
```

### 多 Redis 实例配置

```json
{
  "Redis": {
    "DefaultConnectionString": "localhost:6379",
    "Instances": {
      "cache": "localhost:6379,db=0",
      "session": "localhost:6379,db=1",
      "locks": "redis-cluster:6379,db=2"
    }
  },
  "CacheService": {
    "UseDistributed": true,
    "UseLocal": true,
    "Expiry": "02:00:00",
    "LocalExpiry": "00:30:00",
    "EnableBinarySerialization": true,
    "EnableCompression": true,
    "RedisInstanceName": "cache"
  }
}
```

### 混合缓存配置

```json
{
  "Redis": {
    "DefaultConnectionString": "localhost:6379"
  },
  "CacheService": {
    "UseDistributed": true,
    "UseLocal": true,
    "Expiry": "04:00:00",
    "LocalExpiry": "00:15:00",
    "EnableBinarySerialization": true,
    "EnableCompression": true,
    "EnableLock": true,
    "HideErrors": true,
    "FallbackToFactory": true,
    "LockTimeout": "00:00:02",
    "LockAcquireTimeout": "00:00:02"
  }
}
```

## 代码配置示例

### 使用配置文件

```csharp
// Program.cs 或 Startup.cs
services.AddCaching(configuration);
```

### 使用连接字符串

```csharp
services.AddCaching("localhost:6379", options =>
{
    options.UseDistributed = true;
    options.UseLocal = true;
    options.EnableBinarySerialization = true;
    options.EnableCompression = true;
});
```

### 使用预设配置

```csharp
// 分布式缓存
services.AddCaching("localhost:6379", CacheServiceOptions.Distributed);

// 混合缓存
services.AddCaching("localhost:6379", CacheServiceOptions.Hybrid);

// 高性能缓存
services.AddCaching("localhost:6379", CacheServiceOptions.HighPerformance);

// 生产环境缓存
services.AddCaching(configuration, CacheServiceOptions.Production);
```

### 便利方法

```csharp
// 仅分布式缓存
services.AddDistributedCaching("localhost:6379");

// 仅本地缓存（不需要 Redis）
services.AddLocalCaching();

// 混合缓存
services.AddHybridCaching("localhost:6379");

// 高性能缓存
services.AddHighPerformanceCaching(configuration);

// 开发环境缓存
services.AddDevelopmentCaching(configuration);

// 生产环境缓存
services.AddProductionCaching(configuration);
```

### 多 Redis 实例

```csharp
var redisInstances = new Dictionary<string, string>
{
    ["cache"] = "localhost:6379,db=0",
    ["session"] = "localhost:6379,db=1",
    ["locks"] = "redis-cluster:6379"
};

services.AddCaching(redisInstances, "localhost:6379", options =>
{
    options.RedisInstanceName = "cache";
    options.EnableLock = true;
});
```

### 添加健康检查

```csharp
services.AddCaching(configuration);
services.AddCacheHealthChecks();

// 使用健康检查
public class HealthController : ControllerBase
{
    private readonly ICacheHealthCheck _healthCheck;

    public HealthController(ICacheHealthCheck healthCheck)
    {
        _healthCheck = healthCheck;
    }

    [HttpGet("cache")]
    public async Task<IActionResult> CheckCache()
    {
        var result = await _healthCheck.CheckHealthAsync();
        return result.IsHealthy ? Ok(result) : StatusCode(503, result);
    }

    [HttpGet("cache/all")]
    public async Task<IActionResult> CheckAllCaches()
    {
        var results = await _healthCheck.CheckAllHealthAsync();
        var allHealthy = results.Values.All(r => r.IsHealthy);
        return allHealthy ? Ok(results) : StatusCode(503, results);
    }
}
```

## 使用示例

### 基本使用

```csharp
public class UserService
{
    private readonly ICacheService _cache;

    public UserService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<User> GetUserAsync(int userId)
    {
        return await _cache.GetOrSetAsync(
            $"user:{userId}",
            async () => await _userRepository.GetByIdAsync(userId),
            options =>
            {
                options.Expiry = TimeSpan.FromMinutes(30);
                options.EnableLock = true;
            });
    }
}
```

### 使用不同的 Redis 实例

```csharp
public async Task<string> GetSessionDataAsync(string sessionId)
{
    return await _cache.GetAsync<string>(
        $"session:{sessionId}",
        options => options.RedisInstanceName = "session");
}
```

### Hash 缓存使用

```csharp
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    var fields = new[] { "name", "email", "avatar", "last_login" };
    
    return await _cache.GetOrSetHashAsync(
        $"user_profile:{userId}",
        fields,
        hash => new UserProfile
        {
            Name = hash["name"],
            Email = hash["email"],
            Avatar = hash["avatar"],
            LastLogin = DateTime.Parse(hash["last_login"])
        },
        async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return new Dictionary<string, string>
            {
                ["cache_timestamp"] = DateTime.UtcNow.ToString("O"),
                ["name"] = user.Name,
                ["email"] = user.Email,
                ["avatar"] = user.Avatar,
                ["last_login"] = user.LastLogin.ToString("O")
            };
        });
}
```