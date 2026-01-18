# BuildingBlocks.Caching

基于 StackExchange.Redis 的现代化缓存基库，支持本地缓存、分布式缓存、混合缓存模式，并提供完善的异常处理、降级策略、序列化优化及防缓存击穿机制。

## 主要特性

- **多种缓存模式**: 支持本地缓存（MemoryCache）、分布式缓存（Redis）或混合模式
- **分布式锁**: 基于 Redis 的 Redlock 算法实现，防止缓存击穿
- **高性能序列化**: 支持 MemoryPack 二进制序列化和 JSON 序列化
- **压缩支持**: 可选的 GZip 压缩以节省存储空间
- **异常处理**: 完善的异常处理和降级策略
- **多 Redis 实例**: 支持多个 Redis 实例配置
- **TTL 管理**: 灵活的缓存过期时间配置和动态调整
- **Hash 缓存**: 支持 Redis Hash 数据结构的高级缓存操作
- **健康检查**: 内置缓存健康检查功能
- **预设配置**: 提供多种预设配置模板，快速适应不同场景

## 快速开始

### 1. 安装依赖

确保项目中已安装以下 NuGet 包：

```xml
<PackageReference Include="StackExchange.Redis" />
<PackageReference Include="MemoryPack.Core" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" />
<PackageReference Include="Microsoft.Extensions.Configuration" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
```

### 2. 配置服务

#### 使用配置文件

```csharp
// Program.cs
services.AddCaching(configuration);
```

#### 使用连接字符串

```csharp
services.AddCaching("localhost:6379", options =>
{
    options.UseDistributed = true;
    options.UseLocal = true;
    options.EnableBinarySerialization = true;
    options.EnableCompression = true;
});
```

#### 使用预设配置

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

### 3. 基本使用

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

## 配置选项

### appsettings.json 配置

#### 基本配置

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

#### 多 Redis 实例配置

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

#### 混合缓存配置

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

### 预设配置选项

库提供了多种预设配置，适用于不同的使用场景：

#### CacheServiceOptions.Distributed
- 纯分布式缓存模式
- 启用分布式锁
- 1小时过期时间

#### CacheServiceOptions.Local
- 纯本地缓存模式
- 不使用分布式锁
- 30分钟过期时间

#### CacheServiceOptions.Hybrid
- 混合缓存模式（本地 + 分布式）
- 分布式缓存1小时，本地缓存10分钟
- 启用分布式锁

#### CacheServiceOptions.WithLock
- 启用分布式锁防击穿
- 2秒锁超时时间
- 支持工厂方法降级

#### CacheServiceOptions.HighAvailability
- 高可用性配置
- 启用多种降级策略
- 混合缓存模式

#### CacheServiceOptions.BinarySerialization
- 启用 MemoryPack 二进制序列化
- 启用 GZip 压缩
- 高性能序列化

#### CacheServiceOptions.HighPerformance
- 高性能配置
- 二进制序列化 + 压缩
- 混合缓存 + 分布式锁
- 500ms 快速锁超时

#### CacheServiceOptions.Development
- 开发环境配置
- 不隐藏异常（便于调试）
- 较短的过期时间
- 不启用分布式锁

#### CacheServiceOptions.Production
- 生产环境配置
- 高可用性和性能优化
- 4小时分布式缓存，30分钟本地缓存
- 启用所有优化功能

## 服务注册方式

### 基础注册方法

```csharp
// 使用配置文件
services.AddCaching(configuration);

// 使用连接字符串
services.AddCaching("localhost:6379", options =>
{
    options.UseDistributed = true;
    options.UseLocal = true;
});

// 使用多个 Redis 实例
var redisInstances = new Dictionary<string, string>
{
    ["cache"] = "localhost:6379,db=0",
    ["session"] = "localhost:6379,db=1",
    ["locks"] = "redis-cluster:6379"
};
services.AddCaching(redisInstances, "localhost:6379", options =>
{
    options.RedisInstanceName = "cache";
});
```

### 便利注册方法

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

### 使用预设配置

```csharp
// 分布式缓存
services.AddCaching("localhost:6379", CacheServiceOptions.Distributed);

// 混合缓存
services.AddCaching("localhost:6379", CacheServiceOptions.Hybrid);

// 高性能缓存
services.AddCaching(configuration, CacheServiceOptions.HighPerformance);

// 生产环境缓存
services.AddCaching(configuration, CacheServiceOptions.Production);
```

## 核心功能使用

### 基础缓存操作

```csharp
public class ProductService
{
    private readonly ICacheService _cache;

    public ProductService(ICacheService cache)
    {
        _cache = cache;
    }

    // 获取或设置缓存
    public async Task<Product> GetProductAsync(int productId)
    {
        return await _cache.GetOrSetAsync(
            $"product:{productId}",
            async () => await _productRepository.GetByIdAsync(productId),
            options =>
            {
                options.Expiry = TimeSpan.FromHours(2);
                options.EnableLock = true;
            });
    }

    // 直接设置缓存
    public async Task UpdateProductCacheAsync(Product product)
    {
        await _cache.SetAsync($"product:{product.Id}", product, options =>
        {
            options.Expiry = TimeSpan.FromHours(2);
        });
    }

    // 获取缓存
    public async Task<Product?> GetCachedProductAsync(int productId)
    {
        return await _cache.GetAsync<Product>($"product:{productId}");
    }

    // 删除缓存
    public async Task RemoveProductCacheAsync(int productId)
    {
        await _cache.RemoveAsync($"product:{productId}");
    }

    // 检查缓存是否存在
    public async Task<bool> IsProductCachedAsync(int productId)
    {
        return await _cache.ExistsAsync($"product:{productId}");
    }
}
```

### Hash 缓存操作

Hash 缓存适用于需要部分字段查询和更新的场景：

```csharp
public class UserProfileService
{
    private readonly ICacheService _cache;

    public UserProfileService(ICacheService cache)
    {
        _cache = cache;
    }

    // 获取用户资料（返回强类型）
    public async Task<UserProfile?> GetUserProfileAsync(int userId)
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
            },
            options =>
            {
                options.Expiry = TimeSpan.FromHours(1);
                options.EnableLock = true;
            });
    }

    // 获取用户资料（返回原始字典）
    public async Task<Dictionary<string, string>?> GetUserProfileRawAsync(int userId)
    {
        var fields = new[] { "name", "email", "avatar" };
        
        return await _cache.GetOrSetHashAsync(
            $"user_profile:{userId}",
            fields,
            async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                return new Dictionary<string, string>
                {
                    ["cache_timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["name"] = user.Name,
                    ["email"] = user.Email,
                    ["avatar"] = user.Avatar
                };
            });
    }
}
```

### 使用不同的 Redis 实例

```csharp
public class SessionService
{
    private readonly ICacheService _cache;

    public SessionService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<string?> GetSessionDataAsync(string sessionId)
    {
        return await _cache.GetAsync<string>(
            $"session:{sessionId}",
            options => options.RedisInstanceName = "session");
    }

    public async Task SetSessionDataAsync(string sessionId, string data)
    {
        await _cache.SetAsync($"session:{sessionId}", data, options =>
        {
            options.RedisInstanceName = "session";
            options.Expiry = TimeSpan.FromMinutes(30);
        });
    }
}
```

### TTL 动态调整

```csharp
public class DynamicTtlService
{
    private readonly ICacheService _cache;

    public DynamicTtlService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<ApiResponse> GetApiResponseAsync(string endpoint)
    {
        return await _cache.GetOrSetAsync(
            $"api_response:{endpoint}",
            async () => await _apiClient.GetAsync(endpoint),
            options =>
            {
                options.Expiry = TimeSpan.FromMinutes(30);
                
                // 根据响应内容动态调整 TTL
                options.AdjustExpiryForValue = (originalTtl, value) =>
                {
                    if (value is ApiResponse response)
                    {
                        // 错误响应缓存时间更短
                        if (!response.IsSuccess)
                            return TimeSpan.FromMinutes(5);
                        
                        // 重要数据缓存时间更长
                        if (response.Priority == Priority.High)
                            return TimeSpan.FromHours(2);
                    }
                    
                    return originalTtl;
                };
            });
    }

    public async Task<UserStats> GetUserStatsAsync(int userId)
    {
        var fields = new[] { "login_count", "last_active", "score" };
        
        return await _cache.GetOrSetHashAsync(
            $"user_stats:{userId}",
            fields,
            hash => new UserStats
            {
                LoginCount = int.Parse(hash["login_count"]),
                LastActive = DateTime.Parse(hash["last_active"]),
                Score = int.Parse(hash["score"])
            },
            async () =>
            {
                var stats = await _statsRepository.GetUserStatsAsync(userId);
                return new Dictionary<string, string>
                {
                    ["cache_timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["login_count"] = stats.LoginCount.ToString(),
                    ["last_active"] = stats.LastActive.ToString("O"),
                    ["score"] = stats.Score.ToString()
                };
            },
            options =>
            {
                options.Expiry = TimeSpan.FromHours(1);
                
                // 根据 Hash 内容动态调整 TTL
                options.AdjustExpiryForHash = (originalTtl, hash) =>
                {
                    // 活跃用户的统计数据缓存时间更短
                    if (hash.TryGetValue("last_active", out var lastActiveStr) &&
                        DateTime.TryParse(lastActiveStr, out var lastActive) &&
                        DateTime.UtcNow - lastActive < TimeSpan.FromHours(1))
                    {
                        return TimeSpan.FromMinutes(15);
                    }
                    
                    return originalTtl;
                };
            });
    }
}
```

### 异常处理和降级策略

```csharp
public class ResilientCacheService
{
    private readonly ICacheService _cache;

    public ResilientCacheService(ICacheService cache)
    {
        _cache = cache;
    }

    // 工厂方法降级
    public async Task<WeatherData> GetWeatherDataAsync(string city)
    {
        return await _cache.GetOrSetAsync(
            $"weather:{city}",
            async () => await _weatherApi.GetWeatherAsync(city),
            options =>
            {
                options.Expiry = TimeSpan.FromMinutes(30);
                options.HideErrors = true;
                options.FallbackToFactory = true; // 缓存失败时调用原始 API
                options.EnableLock = true;
            });
    }

    // 默认值降级
    public async Task<UserPreferences> GetUserPreferencesAsync(int userId)
    {
        return await _cache.GetOrSetAsync(
            $"user_preferences:{userId}",
            async () => await _preferencesRepository.GetAsync(userId),
            options =>
            {
                options.Expiry = TimeSpan.FromHours(2);
                options.HideErrors = true;
                options.FallbackToDefault = true;
                options.DefaultValue = UserPreferences.Default; // 使用默认偏好设置
            });
    }

    // 自定义降级函数
    public async Task<string> GetTranslationAsync(string key, string language)
    {
        return await _cache.GetOrSetAsync(
            $"translation:{language}:{key}",
            async () => await _translationService.GetAsync(key, language),
            options =>
            {
                options.Expiry = TimeSpan.FromHours(6);
                options.HideErrors = true;
                options.FallbackFunction = (cacheKey, errorMessage) =>
                {
                    // 自定义降级逻辑：返回键名作为降级值
                    return key;
                };
            });
    }

    // 异常回调处理
    public async Task<CriticalData> GetCriticalDataAsync(string id)
    {
        return await _cache.GetOrSetAsync(
            $"critical_data:{id}",
            async () => await _criticalDataService.GetAsync(id),
            options =>
            {
                options.Expiry = TimeSpan.FromMinutes(15);
                options.HideErrors = false; // 不隐藏异常，让上层处理
                options.OnError = exception =>
                {
                    // 记录关键数据的缓存错误
                    _logger.LogError(exception, "Critical data cache error for ID: {Id}", id);
                    
                    // 发送告警
                    _alertService.SendAlert($"Cache error for critical data: {id}");
                    
                    return null;
                };
            });
    }
}
```

## 分布式锁使用

### 基本用法

```csharp
public class CriticalOperationService
{
    private readonly IDistributedLockProvider _lockProvider;

    public CriticalOperationService(IDistributedLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    public async Task<bool> ProcessOrderAsync(int orderId)
    {
        var lockKey = $"order_processing:{orderId}";
        var lockTimeout = TimeSpan.FromSeconds(30);
        var acquireTimeout = TimeSpan.FromSeconds(5);

        var distributedLock = await _lockProvider.TryAcquireLockAsync(
            lockKey, lockTimeout, acquireTimeout);

        if (distributedLock != null)
        {
            try
            {
                // 执行需要同步的订单处理操作
                await ProcessOrderInternal(orderId);
                return true;
            }
            finally
            {
                // 释放锁
                await distributedLock.ReleaseAsync();
                await distributedLock.DisposeAsync();
            }
        }
        else
        {
            // 获取锁失败，执行降级逻辑
            _logger.LogWarning("无法获取订单处理锁: {OrderId}", orderId);
            return false;
        }
    }
}
```

### 使用 using 语句自动释放锁

```csharp
public async Task<string> GenerateReportAsync(string reportType)
{
    var lockKey = $"report_generation:{reportType}";
    
    await using var distributedLock = await _lockProvider.TryAcquireLockAsync(
        lockKey, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));

    if (distributedLock != null)
    {
        // 执行报告生成操作
        var report = await _reportGenerator.GenerateAsync(reportType);
        
        // 锁会在 using 块结束时自动释放
        return report;
    }
    
    throw new InvalidOperationException($"无法获取报告生成锁: {reportType}");
}
```

### 强制获取锁（抛出异常）

```csharp
public async Task UpdateGlobalConfigAsync(GlobalConfig config)
{
    var lockKey = "global_config_update";
    
    try
    {
        await using var distributedLock = await _lockProvider.AcquireLockAsync(
            lockKey, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
        
        // 执行全局配置更新
        await _configRepository.UpdateAsync(config);
        await _cacheService.RemoveAsync("global_config");
    }
    catch (LockAcquisitionTimeoutException ex)
    {
        // 处理获取锁超时
        _logger.LogError(ex, "获取全局配置更新锁超时");
        throw new InvalidOperationException("系统正忙，请稍后重试", ex);
    }
}
```

### 检查锁状态和强制释放

```csharp
public class LockManagementService
{
    private readonly IDistributedLockProvider _lockProvider;

    public LockManagementService(IDistributedLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    // 检查锁是否被持有
    public async Task<bool> IsResourceLockedAsync(string resourceId)
    {
        var lockKey = $"resource:{resourceId}";
        return await _lockProvider.IsLockHeldAsync(lockKey);
    }

    // 强制释放锁（危险操作，仅在必要时使用）
    public async Task<bool> ForceReleaseLockAsync(string resourceId)
    {
        var lockKey = $"resource:{resourceId}";
        
        _logger.LogWarning("强制释放锁: {LockKey}", lockKey);
        
        return await _lockProvider.ForceReleaseLockAsync(lockKey);
    }

    // 获取锁信息
    public async Task<LockInfo?> GetLockInfoAsync(string resourceId)
    {
        var lockKey = $"resource:{resourceId}";
        return await _lockProvider.GetLockInfoAsync(lockKey);
    }
}
```

### 延长锁的过期时间

```csharp
public async Task<ProcessingResult> ProcessLargeDatasetAsync(string datasetId)
{
    var lockKey = $"dataset_processing:{datasetId}";
    
    await using var distributedLock = await _lockProvider.TryAcquireLockAsync(
        lockKey, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));

    if (distributedLock != null)
    {
        // 开始处理数据
        var result = await StartProcessing(datasetId);
        
        // 如果处理时间可能超过锁的初始超时时间，延长锁
        if (result.EstimatedRemainingTime > TimeSpan.FromMinutes(1))
        {
            var extended = await distributedLock.ExtendAsync(TimeSpan.FromMinutes(5));
            
            if (extended)
            {
                // 继续处理
                return await ContinueProcessing(datasetId, result);
            }
            else
            {
                // 无法延长锁，停止处理
                await StopProcessing(datasetId);
                throw new InvalidOperationException("无法延长处理锁，操作已停止");
            }
        }
        
        return result;
    }
    
    throw new InvalidOperationException($"无法获取数据集处理锁: {datasetId}");
}
```

### 锁的重试机制

```csharp
public class RetryableLockService
{
    private readonly IDistributedLockProvider _lockProvider;

    public RetryableLockService(IDistributedLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        string lockKey,
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        retryDelay ??= TimeSpan.FromMilliseconds(500);
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            await using var distributedLock = await _lockProvider.TryAcquireLockAsync(
                lockKey, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));

            if (distributedLock != null)
            {
                return await operation();
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(retryDelay.Value);
            }
        }

        throw new InvalidOperationException($"在 {maxRetries} 次重试后仍无法获取锁: {lockKey}");
    }
}
```

## 健康检查

### 添加健康检查

```csharp
// 注册健康检查服务
services.AddCaching(configuration);
services.AddCacheHealthChecks();
```

### 使用健康检查

```csharp
[ApiController]
[Route("api/[controller]")]
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

    [HttpGet("cache/detailed")]
    public async Task<IActionResult> GetDetailedCacheHealth()
    {
        var result = await _healthCheck.GetDetailedHealthAsync();
        return Ok(result);
    }
}
```

### 健康检查响应示例

```json
{
  "isHealthy": true,
  "status": "Healthy",
  "checks": {
    "redis_default": {
      "isHealthy": true,
      "responseTime": "00:00:00.0234567",
      "message": "Redis connection is healthy"
    },
    "redis_cache": {
      "isHealthy": true,
      "responseTime": "00:00:00.0156789",
      "message": "Redis instance 'cache' is healthy"
    },
    "memory_cache": {
      "isHealthy": true,
      "responseTime": "00:00:00.0001234",
      "message": "Memory cache is healthy"
    }
  },
  "totalResponseTime": "00:00:00.0392590"
}
```

## 性能优化建议

### 1. 序列化选择

```csharp
// 对于简单对象，JSON 序列化足够
services.AddCaching("localhost:6379", options =>
{
    options.EnableBinarySerialization = false; // 使用 JSON
});

// 对于复杂对象或高频访问，使用二进制序列化
services.AddCaching("localhost:6379", options =>
{
    options.EnableBinarySerialization = true; // 使用 MemoryPack
});
```

### 2. 压缩策略

```csharp
// 对于大对象启用压缩
services.AddCaching("localhost:6379", options =>
{
    options.EnableCompression = true;
    options.EnableBinarySerialization = true;
});
```

### 3. 混合缓存优化

```csharp
// 合理设置本地缓存和分布式缓存的过期时间
services.AddCaching("localhost:6379", options =>
{
    options.UseDistributed = true;
    options.UseLocal = true;
    options.Expiry = TimeSpan.FromHours(2);        // 分布式缓存
    options.LocalExpiry = TimeSpan.FromMinutes(10); // 本地缓存
});
```

### 4. 锁超时优化

```csharp
// 根据操作复杂度调整锁超时时间
services.AddCaching("localhost:6379", options =>
{
    options.EnableLock = true;
    options.LockTimeout = TimeSpan.FromSeconds(5);        // 锁持有时间
    options.LockAcquireTimeout = TimeSpan.FromSeconds(2); // 锁获取超时
});
```

## 最佳实践

### 1. 缓存键命名规范

```csharp
// 使用有意义的前缀和层次结构
var userCacheKey = $"user:profile:{userId}";
var productCacheKey = $"product:details:{productId}";
var sessionCacheKey = $"session:data:{sessionId}";

// 避免使用特殊字符
var invalidKey = "user:profile:123#$%"; // 不推荐
var validKey = "user:profile:123";      // 推荐
```

### 2. 合理设置过期时间

```csharp
// 根据数据更新频率设置过期时间
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    return await _cache.GetOrSetAsync(
        $"user:profile:{userId}",
        async () => await _userRepository.GetProfileAsync(userId),
        options =>
        {
            // 用户资料变更不频繁，可以缓存较长时间
            options.Expiry = TimeSpan.FromHours(2);
        });
}

public async Task<LiveData> GetLiveDataAsync(string dataId)
{
    return await _cache.GetOrSetAsync(
        $"live:data:{dataId}",
        async () => await _liveDataService.GetAsync(dataId),
        options =>
        {
            // 实时数据变更频繁，缓存时间要短
            options.Expiry = TimeSpan.FromMinutes(1);
        });
}
```

### 3. 异常处理策略

```csharp
// 对于关键业务数据，不隐藏异常
public async Task<CriticalBusinessData> GetCriticalDataAsync(string id)
{
    return await _cache.GetOrSetAsync(
        $"critical:{id}",
        async () => await _criticalDataService.GetAsync(id),
        options =>
        {
            options.HideErrors = false; // 让上层处理异常
            options.EnableLock = true;
        });
}

// 对于非关键数据，可以隐藏异常并使用降级
public async Task<RecommendationData> GetRecommendationsAsync(int userId)
{
    return await _cache.GetOrSetAsync(
        $"recommendations:{userId}",
        async () => await _recommendationService.GetAsync(userId),
        options =>
        {
            options.HideErrors = true;
            options.FallbackToDefault = true;
            options.DefaultValue = RecommendationData.Empty;
        });
}
```

### 4. 分布式锁使用场景

```csharp
// 适合使用分布式锁的场景：
// 1. 防止缓存击穿
public async Task<ExpensiveData> GetExpensiveDataAsync(string id)
{
    return await _cache.GetOrSetAsync(
        $"expensive:{id}",
        async () => await _expensiveOperation.ExecuteAsync(id),
        options =>
        {
            options.EnableLock = true; // 防止并发重建缓存
            options.LockTimeout = TimeSpan.FromSeconds(30);
        });
}

// 2. 全局唯一操作
public async Task<string> GenerateUniqueCodeAsync()
{
    var lockKey = "unique_code_generation";
    
    await using var distributedLock = await _lockProvider.TryAcquireLockAsync(
        lockKey, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

    if (distributedLock != null)
    {
        return await _codeGenerator.GenerateUniqueAsync();
    }
    
    throw new InvalidOperationException("无法获取唯一码生成锁");
}
```

## 故障排除

### 常见问题

#### 1. Redis 连接失败

```csharp
// 检查 Redis 连接配置
{
  "Redis": {
    "DefaultConnectionString": "localhost:6379",
    "ConnectTimeout": 5000,
    "ConnectRetry": 3,
    "AbortOnConnectFail": false
  }
}
```

#### 2. 序列化错误

```csharp
// 确保对象可序列化
[MemoryPackable]
public partial class CacheableObject
{
    public string Name { get; set; }
    public int Value { get; set; }
}

// 或者使用 JSON 序列化
services.AddCaching("localhost:6379", options =>
{
    options.EnableBinarySerialization = false; // 使用 JSON
});
```

#### 3. 锁获取超时

```csharp
// 调整锁超时设置
services.AddCaching("localhost:6379", options =>
{
    options.LockTimeout = TimeSpan.FromSeconds(10);        // 增加锁持有时间
    options.LockAcquireTimeout = TimeSpan.FromSeconds(5);  // 增加锁获取超时
});
```

#### 4. 内存缓存过期

```csharp
// 检查本地缓存配置
services.AddCaching("localhost:6379", options =>
{
    options.UseLocal = true;
    options.LocalExpiry = TimeSpan.FromMinutes(30); // 设置合适的本地缓存过期时间
});
```

### 日志和监控

```csharp
// 启用详细日志
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// 使用异常回调监控缓存错误
services.AddCaching("localhost:6379", options =>
{
    options.OnError = exception =>
    {
        // 记录缓存错误
        var logger = serviceProvider.GetRequiredService<ILogger<CacheService>>();
        logger.LogError(exception, "缓存操作发生错误");
        
        // 发送监控告警
        var monitoring = serviceProvider.GetRequiredService<IMonitoringService>();
        monitoring.RecordCacheError(exception);
        
        return null;
    };
});
```

## 注意事项和限制

### 1. 锁的生命周期管理

- **确保释放锁**: 始终在 `finally` 块或 `using` 语句中释放锁，避免死锁
- **合理设置超时**: 根据操作复杂度设置合适的锁超时时间和获取超时时间
- **避免嵌套锁**: 尽量避免在持有一个锁的情况下获取另一个锁

### 2. Redis 连接管理

- **连接池**: StackExchange.Redis 内部使用连接池，无需手动管理连接
- **连接字符串**: 确保 Redis 服务器可用且连接配置正确
- **网络延迟**: 考虑网络延迟对锁性能的影响

### 3. 序列化注意事项

- **MemoryPack 限制**: 使用 MemoryPack 时，对象必须标记 `[MemoryPackable]` 特性
- **JSON 兼容性**: JSON 序列化对所有 .NET 对象都兼容，但性能较低
- **版本兼容性**: 序列化格式变更可能导致缓存数据不兼容

### 4. 性能考虑

- **本地缓存优先**: 混合模式下优先使用本地缓存，减少网络开销
- **批量操作**: 对于大量数据，考虑使用 Hash 缓存减少 Redis 操作次数
- **压缩权衡**: 压缩可以节省存储空间，但会增加 CPU 开销

### 5. 并发控制

- **分布式锁适用场景**: 跨进程的并发控制，单进程内建议使用 `SemaphoreSlim`
- **锁粒度**: 合理设计锁的粒度，避免过粗或过细的锁
- **死锁预防**: 避免循环等待，统一锁的获取顺序

## 版本兼容性

### 支持的 .NET 版本

- **.NET 10.0** (推荐)
- **.NET 9.0**
- **.NET 8.0** (LTS)

### 依赖版本

- **StackExchange.Redis**: 2.8.0+
- **MemoryPack.Core**: 1.21.0+
- **Microsoft.Extensions.Caching.Memory**: 10.0.0+
- **Microsoft.Extensions.Configuration**: 10.0.0+
- **Microsoft.Extensions.DependencyInjection**: 10.0.0+

## 迁移指南

### 从 FreeRedis 迁移

如果您之前使用的是基于 FreeRedis 的缓存库，可以按照以下步骤迁移：

#### 1. 更新依赖

```xml
<!-- 移除旧依赖 -->
<!-- <PackageReference Include="FreeRedis" /> -->

<!-- 添加新依赖 -->
<PackageReference Include="StackExchange.Redis" />
<PackageReference Include="MemoryPack.Core" />
```

#### 2. 更新服务注册

```csharp
// 旧方式
services.AddSingleton<IRedisClient>(provider =>
{
    return new RedisClient("localhost:6379");
});

// 新方式
services.AddCaching("localhost:6379", options =>
{
    options.UseDistributed = true;
    options.EnableLock = true;
});
```

#### 3. 更新接口使用

```csharp
// 旧接口
public interface IOldCacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
}

// 新接口
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, Action<CacheServiceOptions>? configure = null);
    Task SetAsync<T>(string key, T value, Action<CacheServiceOptions>? configure = null);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Action<CacheServiceOptions>? configure = null);
}
```

### 配置迁移

```csharp
// 旧配置
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}

// 新配置
{
  "Redis": {
    "DefaultConnectionString": "localhost:6379",
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AsyncTimeout": 5000
  },
  "CacheService": {
    "UseDistributed": true,
    "UseLocal": false,
    "Expiry": "01:00:00",
    "EnableLock": true
  }
}
```

## 示例项目

完整的示例项目可以在以下位置找到：

- **基础示例**: `Examples/CacheServiceExample.cs`
- **分布式锁示例**: `Examples/DistributedLockExample.cs`
- **TTL 管理示例**: `Examples/TtlManagementExample.cs`
- **配置示例**: `Examples/ConfigurationExample.md`

### 运行示例

```csharp
// 运行所有示例
await TestRunner.RunAllExamplesAsync();

// 运行特定示例
await CacheServiceExample.RunAsync();
await DistributedLockExample.RunAsync();
await TtlManagementExample.RunAsync();
```

## 贡献指南

欢迎贡献代码和提出建议！请遵循以下指南：

### 1. 代码规范

- 使用 C# 编码规范
- 添加适当的 XML 文档注释
- 编写单元测试覆盖新功能

### 2. 提交流程

1. Fork 项目
2. 创建功能分支
3. 编写代码和测试
4. 提交 Pull Request

### 3. 问题报告

- 使用 GitHub Issues 报告问题
- 提供详细的错误信息和重现步骤
- 包含相关的配置和环境信息

## 许可证

本项目采用 MIT 许可证。详情请参阅 LICENSE 文件。

## 更新日志

### v2.0.0 (当前版本)

- **重大更新**: 从 FreeRedis 迁移到 StackExchange.Redis
- **新增**: 混合缓存模式（本地 + 分布式）
- **新增**: 基于 Redis 的分布式锁实现
- **新增**: MemoryPack 高性能二进制序列化支持
- **新增**: GZip 压缩支持
- **新增**: Hash 缓存操作
- **新增**: 动态 TTL 调整
- **新增**: 多种预设配置模板
- **新增**: 完善的异常处理和降级策略
- **新增**: 缓存健康检查功能
- **改进**: 更灵活的服务注册方式
- **改进**: 更好的性能和稳定性

### v1.x.x (旧版本)

- 基于 FreeRedis 的基础缓存功能
- 简单的 Redis 缓存操作
- 基础的异常处理
