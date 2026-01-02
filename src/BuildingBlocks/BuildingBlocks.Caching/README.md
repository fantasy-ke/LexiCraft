# BuildingBlocks.Caching

该组件基于 FreeRedis 提供了强大的缓存抽象，支持分布式缓存（Redis）以及可选的一级本地缓存（Client Side Caching），集成了异步锁和自动序列化。

## 特性

- **多级缓存**：通过 FreeRedis 实现 Redis + 分布式本地缓存同步。
- **强类型 API**：通过 `ICacheManager` 提供异步、类型安全的缓存操作。
- **高性能**：内置 GZip 压缩支持（可选）。
- **容错处理**：支持异常隐藏配置，保证业务稳定性。
- **自动序列化**：默认集成 Newtonsoft.Json 处理复杂对象。

## 配置

在 `appsettings.json` 中配置 Redis 连接：

```json
{
  "RedisCache": {
    "Enable": true,
    "Host": "127.0.0.1",
    "Port": 6379,
    "Password": "",
    "DefaultDb": 0,
    "KeyPrefix": "LexiCraft:",
    "SideCache": {
      "Enable": true,
      "Capacity": 10000,
      "ExpiredMinutes": 30
    }
  }
}
```

## 快速开始

### 1. 注册服务

```csharp
builder.Services.AddRedisCaching("RedisCache");
```

### 2. 使用 ICacheManager

```csharp
public class ProductService(ICacheManager cacheManager)
{
    public async Task<Product?> GetProductAsync(int productId)
    {
        string cacheKey = $"product:{productId}";
        
        // 尝试从缓存获取，如果不存在则通过工厂函数获取并缓存
        return await cacheManager.GetAsync(cacheKey, async () => 
        {
            return await _db.Products.FindAsync(productId);
        }, TimeSpan.FromHours(1));
    }

    public async Task InvalidateCache(int productId)
    {
        await cacheManager.RemoveAsync($"product:{productId}");
    }
}
```

## 核心 API

### ICacheManager

- `GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)`：带缓存回源的数据获取。
- `SetAsync<T>(string key, T value, TimeSpan? expiry = null)`：设置缓存。
- `RemoveAsync(string key)`：移除指定缓存。
- `ExistsAsync(string key)`：判断键是否存在。
- `GetOrSetAsync`：高级获取或设置方法。

## 本地缓存说明

当 `SideCache.Enable` 为 `true` 时，组件将启用 FreeRedis 的客户端缓存功能：
- **一致性**：利用 Redis 的 Pub/Sub 机制，在多实例环境下自动同步本地缓存失效。
- **性能**：热点数据直接从本地内存读取，极大降低 Redis 网络开销。
- **策略**：支持按键前缀过滤 (`KeyFilterCache`)。
