# BuildingBlocks.Caching

`BuildingBlocks.Caching` 是品牌中性的 Redis/MemoryCache 缓存基础类库，统一提供缓存门面、命名 Redis 连接复用、Redis Hash 与有限租期分布式锁。它不承载授权、业务模型、数据库访问、健康检查，也不提供多节点 Redlock。

## 1. 用途与边界

调用方通常只依赖 `ICacheService`、`IDistributedLockProvider`、`IDistributedLock` 和两个选项类型。Redis 存储、连接工厂、序列化器和压缩器均为 `internal` 实现，不是稳定的公共扩展点。

- `ICacheService`：组合本地内存缓存、Redis、TTL、缓存重建锁和错误降级。
- `IDistributedLockProvider`：获取、检查或强制删除单 Redis 实例锁。
- `CacheServiceOptions`：每次调用新建并由配置委托修改，不从配置文件绑定。
- `RedisConnectionOptions`：从 `RedisCache` 配置节绑定默认及命名 Redis 实例。
- 不保证本地缓存跨节点立即失效；需要强一致时关闭 `UseLocal`。
- 不自动续租分布式锁；临界区必须显著短于 `LockTimeout`。

## 2. 依赖关系

项目引用 `BuildingBlocks`，NuGet 包版本由 `Directory.Packages.props` 集中管理：

| 包 | 用途 |
| --- | --- |
| `StackExchange.Redis` | Redis 连接、String/Hash 操作、事务与 Lua 脚本 |
| `Microsoft.Extensions.Caching.Memory` | 进程内一级缓存 |
| `Microsoft.Extensions.Options` | `RedisConnectionOptions` 绑定 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI 注册入口 |
| `Microsoft.Extensions.Logging.Abstractions` | 连接、命中、降级与锁日志 |
| `MemoryPack` | 可选二进制序列化 |

## 3. 目录结构

| 目录 | 职责 |
| --- | --- |
| `Abstractions/` | 公共缓存门面 `ICacheService` |
| `Options/` | 单次调用选项与 Redis 连接选项 |
| `Locking/` | 公共锁契约、异常及内部 Redis 实现 |
| `Redis/` | 内部 Redis 存储边界与实现 |
| `Redis/Connections/` | 命名实例连接解析与 `ConnectionMultiplexer` 复用 |
| `Redis/Serialization/` | JSON、MemoryPack、GZip 工具 |
| `Services/` | 两级缓存、击穿保护、Hash 和降级编排 |
| `Internal/` | 显式命中结果，避免用 `default(T)` 判断是否命中 |
| `Extensions/` | `AddCaching` 注册入口 |
| `Examples/` | 历史示例；在 `csproj` 中通过 `Compile Remove` 排除，不参与编译 |

## 4. 公共 API 速查

### 4.1 `ICacheService`

| 签名 | 语义 |
| --- | --- |
| `GetAsync<T>(key, configure?, ct)` | 本地优先、Redis 次之；未命中或隐藏错误时返回 `default` |
| `SetAsync<T>(key, value, configure?, ct)` | 写入启用的 Redis/本地层；普通 Redis 值支持动态 TTL、序列化与压缩 |
| `RemoveAsync(key, configure?, ct)` | 删除启用的 Redis 键与对应命名实例的本地副本 |
| `ExistsAsync(key, configure?, ct)` | 先查本地，再查 Redis |
| `SetExpirationAsync(key, expiration, configure?, ct)` | 只修改 Redis 键 TTL，不更新已存在的本地缓存项 |
| `GetOrSetAsync<T>(key, factory, configure?, ct)` | 未命中时按需加锁、锁内二次读取、执行工厂并写缓存 |
| `GetOrSetHashAsync<TResult>(hashKey, fields, parser, builder, configure?, ct)` | 读取字段与内部时间戳；过期时重建完整 Hash 并解析 |
| `GetOrSetHashAsync(hashKey, fields, builder, configure?, ct)` | 返回字典的 Hash 重载；结果可能包含 `cache_timestamp` |

`factory` / `builder` 不直接接收 `CancellationToken`；需要取消时应在闭包中传递调用参数。

### 4.2 分布式锁

| 签名 | 语义 |
| --- | --- |
| `TryAcquireLockAsync(key, lease, wait, instance?, ct)` | 成功返回句柄；竞争超时或 Redis 错误返回 `null`；`wait = 0` 仍尝试一次 |
| `AcquireLockAsync(...)` | 空结果转换为 `LockAcquisitionTimeoutException` |
| `IsLockHeldAsync(key, instance?, ct)` | 只检查键是否存在，不验证所有者；Redis 错误返回 `false` |
| `ForceReleaseLockAsync(key, instance?, ct)` | 不校验 owner token 直接删除，可能删除他人有效锁 |
| `IDistributedLock.ReleaseAsync(ct)` | Lua 比较 owner token 后原子删除 |
| `IDistributedLock.ExtendAsync(lease, ct)` | Lua 比较 owner token 后原子重设 TTL；不会自动调用 |
| `IDistributedLock.IsValid` | 只看本地句柄状态和本地到期时刻，不查询 Redis |

推荐用 `await using` 释放句柄；释放、续期时锁已过期或所有权变化会返回 `false`。

### 4.3 内部 Redis API

`IRedisCacheStore` 提供 `GetAsync`、`SetAsync`、`RemoveAsync`、`ExistsAsync`、`SetExpirationAsync`、`HashGetAsync`、`HashSetAsync`；`IRedisConnectionFactory` 提供默认实例和命名实例的 `GetDatabase`。它们是 `internal`，调用方不应直接解析。

序列化/压缩实现也是 `internal`：默认 `JsonCacheSerializer`，可选 `MemoryPackCacheSerializer`，启用压缩时对序列化后超过 1024 字节的普通 String 值使用 `GZipCacheCompressor`。Hash 字段始终按字符串存取。

## 5. 注册与配置

```csharp
using BuildingBlocks.Caching.Extensions;

builder.Services.AddCaching(builder.Configuration);
```

`AddCaching(IConfiguration)` 先绑定 `RedisCache`，再以 singleton 注册 `IRedisConnectionFactory`、`IRedisCacheStore`、`IDistributedLockProvider`、`ICacheService`，最后调用 `AddMemoryCache()`。也可使用：

```csharp
services.AddCaching("redis-host:6379,abortConnect=false");
services.AddCaching(
    new Dictionary<string, string> { ["Reporting"] = "reporting-redis:6379,abortConnect=false" },
    "redis-host:6379,abortConnect=false");
```

连接工厂构造时要求存在 `default` 连接，即使业务只选择本地缓存。连接在实例首次使用时同步建立，每个实例名在进程内共享一个 `ConnectionMultiplexer`；失败条目会被移除，以便后续重试。

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
    "AbortOnConnectFail": false,
    "EnableConnectionPooling": true,
    "MaxConnectionPoolSize": 10
  }
}
```

| `RedisConnectionOptions` | 默认值 | 说明 |
| --- | --- | --- |
| `DefaultConnectionString` | `null` | `default` 实例；实际注册要求非空，或 `Instances` 包含 `default` |
| `Instances` | 空字典 | 命名实例到连接字符串的映射 |
| `ConnectTimeout` | `5000` ms | 连接字符串未指定 `connectTimeout` 时使用 |
| `SyncTimeout` | `5000` ms | 未指定 `syncTimeout` 时使用 |
| `AsyncTimeout` | `5000` ms | 未指定 `asyncTimeout` 时使用 |
| `ConnectRetry` | `3` | 未指定 `connectRetry` 时使用 |
| `AbortOnConnectFail` | `false` | 未指定 `abortConnect` 时使用 |
| `EnableConnectionPooling` | `true` | 旧配置兼容字段，当前不参与运行时行为 |
| `MaxConnectionPoolSize` | `10` | 旧配置兼容字段，当前不参与运行时行为 |

连接字符串中的密码必须来自环境变量、用户机密或部署平台密钥管理。

`CacheServiceOptions` 是逐调用选项：

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `UseDistributed` | `true` | 使用 Redis |
| `UseLocal` | `false` | 使用进程内缓存；Hash API 不使用本地层 |
| `Expiry` | 180 分钟 | Redis TTL，也是 Hash 时间戳逻辑有效期；非正值回退默认值 |
| `LocalExpiry` | `null` | 本地 TTL；空值或非正值继承 `Expiry` |
| `HideErrors` | `true` | 隐藏非取消异常并走降级；`false` 时包装为 `InvalidOperationException` |
| `EnableCompression` | `false` | 普通 Redis String 超过 1024 字节时 GZip |
| `EnableBinarySerialization` | `false` | 使用 MemoryPack 代替 JSON |
| `EnableLock` | `true` | `GetOrSet` 未命中时使用 Redis 锁 |
| `LockTimeout` | 1 秒 | 锁租期，不自动续期 |
| `LockAcquireTimeout` | 1 秒 | 最长等待；零仍立即尝试一次 |
| `FallbackToFactory` | `true` | 锁失败时直接执行工厂，可能并发重建 |
| `FallbackToDefault` | `false` | 尝试返回 `DefaultValue` |
| `DefaultValue` | `null` | 类型兼容时的默认降级值 |
| `FallbackFunction` | `null` | 参数为键和操作名的自定义降级函数 |
| `OnError` | `null` | 非取消异常回调；兼容返回类型时优先作为结果 |
| `AdjustExpiryForHash` | `null` | 按完整 Hash 调整物理 TTL；失败或非正值回退 `Expiry` |
| `AdjustExpiryForValue` | `null` | 按普通值调整 Redis TTL；不改变本地 TTL |
| `RedisInstanceName` | `null` | 空值表示 `default`；同时隔离本地缓存内部键 |

`Distributed`、`Local`、`Hybrid`、`WithLock`、`HighAvailability`、`BinarySerialization`、`HighPerformance`、`Development`、`Production` 是返回新对象的预设；当前公共调用接受配置委托，不能直接把预设实例传入，调用方仍需显式设置所需属性。

## 6. 最小使用示例

```csharp
public sealed class WordLookupService(
    ICacheService cache,
    IDistributedLockProvider locks)
{
    public Task<WordDto?> GetAsync(Guid id, CancellationToken ct) =>
        cache.GetOrSetAsync(
            $"word:{id:N}",
            () => LoadWordAsync(id, ct),
            options =>
            {
                options.UseDistributed = true;
                options.UseLocal = true;
                options.Expiry = TimeSpan.FromMinutes(30);
                options.LocalExpiry = TimeSpan.FromMinutes(2);
                options.RedisInstanceName = "Reporting";
            },
            ct);

    public Task SetAsync(WordDto word, CancellationToken ct) =>
        cache.SetAsync($"word:{word.Id:N}", word, cancellationToken: ct);

    public Task<Dictionary<string, string>?> GetSummaryAsync(Guid id, CancellationToken ct) =>
        cache.GetOrSetHashAsync(
            $"word-summary:{id:N}",
            ["word", "definition"],
            () => BuildSummaryAsync(id, ct),
            cancellationToken: ct);

    public async Task RebuildAsync(Guid id, CancellationToken ct)
    {
        await using var handle = await locks.AcquireLockAsync(
            $"word-rebuild:{id:N}",
            lockTimeout: TimeSpan.FromSeconds(5),
            acquireTimeout: TimeSpan.FromSeconds(1),
            cancellationToken: ct);

        await RebuildIndexAsync(id, ct);
    }
}
```

## 7. 主流程与一致性语义

### 两级缓存

1. `GetAsync` 先查本地键 `local:{instance}:{businessKey}`。
2. 本地未命中再查 Redis；Redis 命中后按 `LocalExpiry ?? Expiry` 回填本地。
3. `SetAsync` 先写 Redis，再写本地。Redis 写失败且 `HideErrors = true` 时整个设置方法降级返回，本地不会继续写。
4. `RemoveAsync` 先删 Redis，再删本地；Redis 删除返回 `false` 会使最终结果为 `false`，但仍会移除本地副本。Redis 抛异常时本地删除不会执行。
5. `SetExpirationAsync` 只更新 Redis，不改变已加载的本地项。

本地缓存没有 pub/sub 失效广播，跨节点可在本地 TTL 窗口内读到旧值。

### 缓存击穿保护

`GetOrSetAsync` 未命中后尝试获取 `lock:{key}`；成功后使用相同实例、序列化和 TTL 选项二次读取，再执行工厂。锁获取使用单调时钟，每 50ms 或剩余时间（取较小者）重试。Redis 锁错误由 `TryAcquireLockAsync` 记录后转换为 `null`，随后按 `FallbackToFactory` 和其他降级选项处理。

owner token 由机器名、进程 ID、GUID 和 UTC ticks 组成。释放与续期使用 Lua 比较 token，防止旧句柄删除新所有者的锁。这是单实例互斥，不是 Redlock，也不自动续期。

### Hash 与 TTL

Hash 读取总会附加 `cache_timestamp`。若时间戳存在且可解析，年龄达到 `Expiry` 即逻辑过期；缺失或格式无效则按有效处理。重建写入完整 Hash 和 UTC ISO 8601 时间戳。

Redis TTL 只能作用于整个 key，不能作用于单个 field。`HashSetAsync` 在同一 Redis 事务中提交 `HashSet` 和 `KeyExpire`，任一字段写入都会重设整个 Hash 的 TTL。`AdjustExpiryForHash` 只调整物理 TTL，不改变时间戳使用的逻辑有效期。

### 序列化兼容性

默认 JSON；开启 MemoryPack 或压缩后，同一键的读写必须保持一致设置。缓存值没有显式的序列化/压缩格式标记；启用压缩的读取会先尝试 GZip，失败后按原始字节反序列化。切换 MemoryPack、修改其数据契约或切换序列化方式时，应变更键名/版本前缀或清理旧缓存。

## 8. 取消、异常与安全语义

| 场景 | 行为 |
| --- | --- |
| 调用方取消 | `OperationCanceledException` 原样传播，不受 `HideErrors` 影响 |
| `WaitAsync(ct)` 取消 | 只中断本地等待；已发送的 Redis 命令仍可能在服务端完成 |
| Redis 连接、超时或序列化失败，`HideErrors = true` | `ICacheService` 记录错误，按 `OnError`、默认值、自定义函数顺序降级，最后返回 `default` / `false` |
| 同上，`HideErrors = false` | 抛 `InvalidOperationException` 并保留原异常为 inner exception |
| `TryAcquireLockAsync` 竞争超时或 Redis 错误 | 返回 `null` |
| `AcquireLockAsync` 未取得锁 | 抛 `LockAcquisitionTimeoutException`；目前不能仅从异常区分竞争与 Redis 错误 |
| 锁释放/续期时已过期、所有权变化或 Redis 错误 | 返回 `false`；取消仍传播 |
| `ExtendAsync` 的时长非正 | 抛 `ArgumentException` |
| 未配置默认 Redis | 解析连接工厂时抛 `InvalidOperationException` |
| 序列化或压缩格式不兼容 | 内部 Redis 存储抛异常，再由 `HideErrors` 决定包装或降级 |
| `ForceReleaseLockAsync` | 不校验所有者；只能由已确认安全的运维/恢复流程调用 |

不要把凭据、令牌或敏感个人数据直接作为缓存键或结构化日志字段；业务键应有稳定前缀和必要的哈希/脱敏策略。

## 9. 已知限制与技术债

1. 首次使用通过同步 `ConnectionMultiplexer.Connect` 建连，未提供启动预热或异步连接。
2. 构造连接工厂强制要求默认实例，纯本地缓存场景仍需提供 Redis 配置。
3. 工厂委托不直接接收 `CancellationToken`，只能通过闭包传递。
4. 分布式锁没有自动续期，也不提供 Redlock；`IsValid` 只是本地判断。
5. `TryAcquireLockAsync` 将 Redis 错误和竞争超时都映射为空，严格故障诊断需结合日志。
6. 本地缓存没有跨进程失效广播；`SetExpirationAsync` 不更新本地 TTL。
7. Hash 的逻辑时间戳缺失或损坏时按有效处理，且保留字段名 `cache_timestamp` 可能与业务冲突。
8. 序列化和压缩格式没有版本标记；协议切换需通过键版本管理。
9. `EnableConnectionPooling`、`MaxConnectionPoolSize` 仅为旧配置兼容字段。
10. 本类库不注册 Redis 健康检查；宿主应按实际实例补充。
11. `Examples/` 不参与编译，不能作为 API 或测试覆盖证明。

## 10. 测试与验证

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.Caching\BuildingBlocks.Caching.csproj --no-incremental
dotnet test src\BuildingBlocks\BuildingBlocks.Caching.Tests\BuildingBlocks.Caching.Tests.csproj
dotnet build src\Fantasy.slnx
dotnet test src\Fantasy.Tests.slnx
git diff --check
```

集成测试使用 Testcontainers/真实 Redis 时需要可用的 Docker 环境；测试被跳过不等于通过，应检查测试输出中的跳过数量与原因。
