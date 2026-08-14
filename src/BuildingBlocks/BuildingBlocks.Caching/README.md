# BuildingBlocks.Caching

`BuildingBlocks.Caching` 是 LexiCraft 的 Redis/MemoryCache 缓存基础类库。它只提供缓存、Redis 连接复用和分布式锁能力，不承载授权、业务模型或数据库访问。

## 类库边界

| 区域 | 职责 | 是否为公共契约 |
| --- | --- | --- |
| `Abstractions/` | `ICacheService` 业务缓存门面 | 是 |
| `Options/` | 每次调用的 `CacheServiceOptions`、全局 `RedisConnectionOptions` | 是 |
| `Locking/` | 分布式锁接口、异常和 Redis 实现 | 接口/异常是；实现否 |
| `Redis/` | Redis 存取、连接与序列化实现 | 否 |
| `Services/` | 混合缓存编排实现 | 否 |
| `Extensions/` | 依赖注入注册入口 | 是 |
| `Examples/` | 历史示例，仅作参考，不参与运行时编译 | 否 |

调用方应依赖 `ICacheService`、`IDistributedLockProvider` 和选项模型，不应直接依赖 Redis 存储、连接工厂或序列化工具。

## 注册

```csharp
using BuildingBlocks.Caching.Extensions;

builder.Services.AddCaching(builder.Configuration);
```

默认读取 `RedisCache` 配置：

```json
{
  "RedisCache": {
    "DefaultConnectionString": "redis-host:6379,abortConnect=false",
    "Instances": {
      "Reporting": "reporting-redis:6379,abortConnect=false"
    },
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AsyncTimeout": 5000,
    "ConnectRetry": 3,
    "AbortOnConnectFail": false
  }
}
```

连接字符串中的密码必须由环境变量、用户机密或部署平台密钥管理提供，不应提交到仓库。

也可以直接传入默认连接字符串或命名实例字典：

```csharp
services.AddCaching(connectionString);
services.AddCaching(instances, defaultConnectionString);
```

## 基本使用

```csharp
public sealed class WordLookupService(ICacheService cache)
{
    public Task<WordDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return cache.GetOrSetAsync(
            $"word:{id:N}",
            () => LoadWordAsync(id, cancellationToken),
            options =>
            {
                options.UseDistributed = true;
                options.UseLocal = true;
                options.Expiry = TimeSpan.FromMinutes(30);
                options.LocalExpiry = TimeSpan.FromMinutes(2);
            },
            cancellationToken);
    }
}
```

`CacheServiceOptions` 还提供 `Distributed`、`Local`、`Hybrid`、`WithLock`、`HighAvailability`、`BinarySerialization`、`HighPerformance`、`Development` 和 `Production` 预设。预设返回新对象，调用方仍应根据数据一致性和容量要求显式调整。

## 关键语义

### 命中与未命中

底层 Redis 读取使用显式命中结果，不能再用 `default(T)` 判断是否命中。因此 `0`、`false` 等非空值类型不会被误判为缓存数据。

### 本地与分布式缓存

- 本地缓存键包含 Redis 实例名，避免同一业务键在不同 Redis 实例间串值。
- 启用混合缓存时先读取本地缓存，再读取 Redis。
- 删除和过期时间更新会同步处理已知的本地缓存键。
- 本地缓存仅适合允许短时间节点内陈旧的数据；跨实例强一致场景应关闭本地缓存。

### 防击穿锁

- 锁实现基于 Redis 单实例的带租期互斥键和 Lua 所有权校验，不是 Redlock。
- 获取超时为零时仍进行一次非阻塞尝试。
- 获取等待使用单调时钟，并按剩余时间限制重试延迟。
- 锁内二次读取沿用首次调用解析出的完整选项，不会丢失 Redis 实例、序列化或 TTL 设置。

### 异常与取消

- 调用方触发的 `OperationCanceledException` 始终向上传播，不受 `HideErrors` 影响。
- `HideErrors` 只控制依赖异常的降级行为，不能把取消伪装成缓存未命中。
- 需要严格一致性的调用应设置 `HideErrors = false`，并明确工厂回退策略。

### Hash 缓存

- Hash 写入和过期时间设置在同一 Redis 事务中提交，避免只写数据未设置 TTL 的永久键。
- 内部 `cache_timestamp` 字段用于判断重建时间，即使调用方只请求部分字段也会读取该字段。
- 动态 TTL 回调返回非正值时回退到基础 TTL。
- `cache_timestamp` 是内部字段，业务字段不要使用同名键。

## 序列化与压缩

- 默认使用 `System.Text.Json`。
- `EnableBinarySerialization` 使用 MemoryPack；缓存 DTO 必须满足 MemoryPack 的序列化要求。
- `EnableCompression` 只对超过内部阈值的数据使用 GZip。
- 序列化器和压缩器是内部实现，不属于稳定公共 API。

## 当前限制

1. Redis 连接在首次使用时通过同步 `ConnectionMultiplexer.Connect` 创建；启动预热和异步连接是后续独立优化项。
2. `Func<Task<T>>` 工厂没有直接接收 `CancellationToken`；调用方当前应在闭包中传递令牌，后续可增加兼容重载。
3. 分布式锁没有自动续租，临界区必须显著短于 `LockTimeout`。
4. `EnableConnectionPooling`、`MaxConnectionPoolSize` 为旧配置兼容字段；StackExchange.Redis 使用共享 `ConnectionMultiplexer`，这两个字段不生效并已标记废弃。
5. 本类库当前不提供健康检查注册；应由宿主按实际 Redis 实例和部署策略配置健康检查。
6. `Examples/` 不参与编译，不能作为 API 正确性的证明。

## 验证

```powershell
dotnet test src\BuildingBlocks\BuildingBlocks.Caching.Tests\BuildingBlocks.Caching.Tests.csproj
dotnet build src\LexiCraft.slnx --no-restore
git diff --check
```