# BuildingBlocks.MongoDB

该组件提供 MongoDB 上下文、实体基类、通用仓储、事务会话管理、读取弹性和有界性能指标。

## 目录职责

- `Abstractions/`：`IMongoDbContext`。
- `Context/`：上下文和 session 生命周期。
- `Entities/`：Mongo 实体与聚合根基类。
- `Repositories/`：统一的读/写仓储层级。
- `Resilience/`：Mongo 专属弹性端口与实现。
- `Performance/`：轻量进程内指标。
- `Errors/`、`Serialization/`、`Configuration/`、`Extensions/`：对应的适配器与组合根。

## 注册

```csharp
builder.AddResilience();
builder.AddMongoDbContext<PracticeDbContext>(nameof(MongoOptions));
```

如果需要按实体自动注册默认 `IQueryRepository<TEntity>` / `IRepository<TEntity>`，再调用：

```csharp
builder.AddMongoRepository<PracticeDbContext>();
```

自定义仓储可以直接继承 `MongoQueryRepository<TEntity>` 或 `MongoRepository<TEntity>` 并显式指定集合名。

传入的配置节名称会同时用于 Options 注册和即时绑定，不会回退到固定的 `MongoOptions` 节。连接字符串必须包含数据库名，连接池边界会在启动时校验。

## BSON 约定

进程内只注册一次以下全局约定，避免多个 Host/TestServer 重复注册异常，并保持嵌套文档的既有格式：

- 属性名使用 camelCase；
- 枚举存储为字符串；
- 忽略额外字段；
- `DateTime` 按 UTC 序列化；
- Guid 保持现有 CSharpLegacy 表示以兼容历史数据。

## 单一仓储管线

原先“支持事务的普通仓储”和“支持重试/监控的 Resilient 仓储”是两套平行继承树，导致业务仓储只能二选一。现在统一为：

- `MongoQueryRepository<TEntity>`：读取、监控、非事务读取重试和 session 绑定；
- `MongoRepository<TEntity>`：在同一管线之上增加即时写入。

所有异步方法均接受 `CancellationToken`。单条查询最多读取 1 条，`Single*` 最多读取 2 条；非事务分页并行执行计数和列表查询，事务内使用同一 session 串行执行。

`MongoDbContext` 禁止同一 scoped context 嵌套开启事务；提交或回滚成功后释放 session，操作失败时保留 session，供调用方继续完成事务收尾。

批量写入先物化一次，空集合直接返回。MongoDB 写操作即时生效，因此兼容接口 `SaveChangesAsync()` 返回 `0`，不能作为受影响文档数。

## 重试边界

- 非事务读取：通过 `IMongoResilienceService` 对明确 transient 的连接/超时异常进行应用层重试。
- 事务内读取：不重试单条语句，事务必须作为完整单元重试，避免部分操作被重复执行。
- 写入：不做应用层自动重试，依赖 MongoDB 驱动的 retryable writes；业务仍需唯一索引、幂等键或事务保护。

Mongo 使用专属 `IMongoResilienceService` 注册，不再占用全局 `IResilienceService` 服务槽位，避免同一进程中的其他弹性实现被覆盖。

## 性能指标

`MongoPerformanceMonitor` 在关闭时不分配计时器；开启时在并发记录下最多保留 10,000 条进程内指标，慢操作超过 200ms 记录 Warning，超过 1s 记录 Error。

该指标用于轻量诊断，不代替 OpenTelemetry 或生产时序指标系统。