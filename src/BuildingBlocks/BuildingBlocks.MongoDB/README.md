# BuildingBlocks.MongoDB

该组件提供 MongoDB 上下文、通用仓储、事务会话管理、有限重试和有界性能指标。

## 注册

先注册通用 resilience 配置，再注册 MongoDB：

```csharp
builder.AddResilience();
builder.AddMongoDbContext<PracticeDbContext>(nameof(MongoOptions));
```

传入的配置节名称会同时用于 Options 注册和即时绑定，不会回退到固定的 `MongoOptions` 节。

```json
{
  "MongoOptions": {
    "ConnectionString": "mongodb://localhost:27017/lexicraft_practice",
    "MaxConnectionPoolSize": 100,
    "MinConnectionPoolSize": 10,
    "MaxConnectionIdleTime": "00:10:00",
    "MaxConnectionLifeTime": "00:30:00",
    "ConnectTimeout": "00:00:30",
    "SocketTimeout": "00:01:00",
    "ServerSelectionTimeout": "00:00:30",
    "DisableTracing": false,
    "EnablePerformanceMonitoring": true
  }
}
```

连接字符串必须包含数据库名；连接池大小在启动时校验。

## BSON 约定

进程内只注册一次以下全局约定，避免多个 Host/TestServer 重复注册异常，并保持嵌套文档的既有格式：

- 属性名使用 camelCase；
- 枚举存储为字符串；
- 忽略额外字段；
- `DateTime` 按 UTC 序列化；
- Guid 保持现有 CSharpLegacy 表示以兼容历史数据。

## 仓储与事务

所有异步仓储方法均接受 `CancellationToken`。单条查询增加了 `Limit(1)`，`Single*` 增加 `Limit(2)`；非事务分页并行执行计数和列表查询，事务内查询统一绑定当前 session，并按 MongoDB 驱动约束串行执行。分页参数会在发起查询前校验。

`MongoDbContext` 禁止同一 scoped context 嵌套开启事务；提交或回滚成功后释放 session，操作失败时保留 session，供调用方按事务语义重试提交或回滚。

批量写入先物化一次，空集合直接返回，不向驱动发送无效批次。MongoDB 写操作是即时落库，因此 `SaveChangesAsync` 是兼容接口并返回 `0`，不能把它当作实际受影响文档数。

## 重试边界

仅对连接异常、执行超时、`TimeoutException` 和 MongoDB 标记为 transient/retryable 的异常重试。认证、序列化、参数和普通 `MongoException` 不重试。

> 写操作重试仍要求业务层提供幂等键、唯一索引或事务语义。没有幂等保护时，网络超时发生在服务端已成功写入之后，客户端重试可能造成重复效果。

## 性能指标

`MongoPerformanceMonitor` 在关闭时不分配计时器；开启时在并发记录下最多保留 10,000 条进程内指标，慢操作超过 200ms 记录 Warning，超过 1s 记录 Error。

```csharp
var metrics = await monitor.GetMetricsAsync(TimeSpan.FromMinutes(5));
```

该指标用于轻量诊断，不代替 OpenTelemetry 或生产时序指标系统。