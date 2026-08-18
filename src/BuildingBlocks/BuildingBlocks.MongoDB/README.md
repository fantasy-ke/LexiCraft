# BuildingBlocks.MongoDB

`BuildingBlocks.MongoDB` 是 `BuildingBlocks.Persistence.Abstractions` 的 MongoDB 适配器，提供客户端与数据库注册、作用域 session/事务上下文、统一读写仓储、事务外读取弹性、BSON 约定、ObjectId 辅助、Problem Details 映射和有界进程内性能指标。

## 1. 用途与边界

本类库提供：

- `IMongoDbContext` / `MongoDbContext`：单个 scoped context 内一个活动 session 的生命周期。
- `MongoQueryRepository<TEntity>`：查询、分页、session 绑定、事务外重试和性能计时。
- `MongoRepository<TEntity>`：在查询管线之上增加即时插入、替换和物理删除。
- `MongoEntity` / `MongoAggregateRoot`：`ObjectId` 主键、UTC 创建时间和聚合根标记。
- `IMongoResilienceService`：MongoDB 专属弹性端口，避免占用全局 `IResilienceService` 槽位。
- `MongoDbProblemCodeMapper`：常见 MongoDB 异常到 HTTP 状态码的映射。

它不提供 EF Core 风格 ChangeTracker、延迟 `SaveChanges`、通用软删除、自动索引迁移、事务完整单元自动重试、outbox、领域事件派发或跨实例性能指标。业务层应依赖 `IQueryRepository<TEntity>` / `IRepository<TEntity>`；只有需要稳定集合名、MongoDB 原生查询或 session 扩展时才继承具体仓储。

## 2. 依赖关系

项目引用：

| 引用 | 用途 |
| --- | --- |
| `BuildingBlocks` | `IAggregateRoot`、通用弹性基类、配置与 Problem Details 基础能力 |
| `BuildingBlocks.Persistence.Abstractions` | 查询仓储与写仓储契约 |

NuGet 包：

| 包 | 用途 |
| --- | --- |
| `MongoDB.Driver` | 客户端、session、事务、集合与 BSON |
| `MongoDB.Driver.Core.Extensions.DiagnosticSources` | MongoDB Driver Activity 追踪 |
| `Microsoft.Extensions.Options` | MongoDB 与弹性选项 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 上下文和仓储注册 |
| `Microsoft.Extensions.Configuration.Abstractions` | 配置节绑定 |

## 3. 目录结构

| 目录 | 职责 |
| --- | --- |
| `Abstractions/` | `IMongoDbContext` 数据库、客户端与 session 契约 |
| `Configuration/` | `MongoOptions` |
| `Context/` | `MongoDbContext` 事务 session 生命周期 |
| `Entities/` | `MongoEntity` 与 `MongoAggregateRoot` |
| `Repositories/` | 统一查询仓储与即时写仓储 |
| `Resilience/` | MongoDB 专属瞬时故障判断、读取重试与 ping |
| `Performance/` | 有界进程内操作计时与汇总 |
| `Errors/` | MongoDB 异常状态码映射 |
| `Serialization/` | `DateTime` UTC 序列化提供程序 |
| `Extensions/` | DI 组合根、仓储扫描、ObjectId 扩展 |

## 4. 公共 API 速查

### 4.1 上下文与事务

| 签名 | 语义 |
| --- | --- |
| `IMongoDbContext.Database` | 当前应用数据库 |
| `IMongoDbContext.Client` | 共享客户端，用于创建 session |
| `IMongoDbContext.Session` | 当前 session；没有活动事务时为 `null` |
| `BeginTransactionAsync(ct)` | 创建 session 并开始事务；禁止同一 context 嵌套事务 |
| `CommitTransactionAsync(ct)` | 有活动事务则提交；成功后释放 session；无事务时 no-op |
| `RollbackTransactionAsync(ct)` | 有活动事务则回滚；成功后释放 session；无事务时 no-op |
| `Dispose()` | 释放当前 session，不提交或回滚业务操作 |

提交或回滚失败时 session 被保留，调用方可以决定重试提交、回滚或记录人工恢复信息。

### 4.2 实体与 ObjectId

| 类型/方法 | 语义 |
| --- | --- |
| `MongoEntity.Id` | 新实例默认生成 `ObjectId` |
| `MongoEntity.CreationTime` | 默认 UTC；写仓储插入时覆盖为当前 UTC |
| `MongoEntity.CreatorId` | 可选长整型创建者标识 |
| `MongoAggregateRoot` | 实现 `IAggregateRoot`，因此可注册通用写仓储 |
| `string?.IsValidMongoId()` | 判断非空字符串能否解析为 `ObjectId` |
| `EnsureValidMongoId(id, paramName)` | 返回有效原字符串；无效时抛 `ArgumentException` |
| `FirstOrDefaultByIdAsync(repository, id, ct)` | 校验并按 `ObjectId` 查询 |
| `GetByIdAsync(repository, id, ct)` | 上一方法的语义化别名 |

全局 Guid serializer 使用 `GuidRepresentation.CSharpLegacy`，这是历史数据兼容约束。不要在没有数据迁移和双读验证的情况下切换表示方式。

### 4.3 仓储

`MongoQueryRepository<TEntity>` 实现 `IQueryRepository<TEntity>`：

| 成员 | 语义 |
| --- | --- |
| `GetListAsync(predicate, ct)` / `GetListAsync(ct)` | 条件列表 / 无界全量列表 |
| `FirstOrDefaultAsync` / `FirstAsync` | 最多取 1 条；严格重载无结果时抛异常 |
| `SingleOrDefaultAsync` / `SingleAsync` | 最多取 2 条，用于检测多条 |
| `CountAsync` / `AnyAsync` / `GetAsync` | 计数、存在性、首条别名 |
| `Query()` / `QueryNoTracking()` | MongoDB 无 ChangeTracker，两者等价；事务中绑定当前 session |
| `GetPageListAsync(predicate, page, size, orderBy?, isAsc, ct)` | 校验分页；事务外并行计数和取页，事务内串行 |

`MongoRepository<TEntity>` 仅适用于 `MongoEntity + IAggregateRoot`：

| 成员 | 语义 | 写入时机 |
| --- | --- | --- |
| `InsertAsync(entity, ct)` | 覆盖 UTC 创建时间并插入 | 方法内即时写入 |
| `InsertAsync(entities, ct)` | 物化一次；空集合 no-op；共享同一创建时间 | 方法内即时批量写入 |
| `UpdateAsync(entity, ct)` | 按 `Id` 替换整份文档；无匹配时抛异常 | 方法内即时写入 |
| `DeleteAsync(entity, ct)` | 按 `Id` 物理删除单文档；无匹配正常完成 | 方法内即时写入 |
| `DeleteAsync(predicate, ct)` | 物理删除全部匹配文档 | 方法内即时写入 |
| `SaveChangesAsync(ct)` | 兼容抽象，固定返回 `0` | 不发送数据库命令 |

默认集合名是实体 CLR 类型名，不做复数化、camelCase 或 snake_case。线上集合名必须稳定时，创建业务仓储并通过 protected 构造函数显式传入集合名。

### 4.4 弹性、错误与监控

| API | 语义 |
| --- | --- |
| `IMongoResilienceService.ExecuteWithRetryAsync(...)` | 由通用弹性基类执行；内置仓储只在事务外读取调用 |
| `IMongoResilienceService.IsHealthyAsync(ct)` | ping `admin` 数据库；非取消异常返回 `false` |
| `MongoDbProblemCodeMapper.GetMappedStatusCodes(ex)` | 连接 503、超时 408、写入/命令错误 500，其他交给默认映射器 |
| `IMongoPerformanceMonitor.StartOperation(name, collection)` | 开始计时；关闭监控时返回空句柄 |
| `GetMetricsAsync(period?)` | 汇总窗口内总数、平均/最大/最小耗时、每秒操作数、慢操作和分组 |

## 5. 注册顺序与扩展方法

```csharp
using BuildingBlocks.MongoDB.Context;
using BuildingBlocks.MongoDB.Extensions;

public sealed class PracticeDbContext(
    IMongoDatabase database,
    IMongoClient client) : MongoDbContext(database, client);

builder.AddMongoDbContext<PracticeDbContext>("PracticeMongo");
builder.AddMongoRepository<PracticeDbContext>();
```

`AddMongoDbContext<TContext>(sectionName)`：

1. 绑定并即时校验指定配置节；节名为空时使用 `MongoOptions`。
2. 要求连接字符串非空、格式有效且包含数据库名；校验连接池上下界。
3. 线程安全地执行一次全局 BSON serializer/convention 注册。
4. 注册 singleton `IMongoClient`、`IMongoDatabase`、`IMongoPerformanceMonitor`。
5. 注册 scoped `IMongoResilienceService`、具体 `TContext` 和 `IMongoDbContext`。
6. 用 `MongoDbProblemCodeMapper` 替换 `IProblemCodeMapper`。

`AddMongoRepository<TDbContext>()` 扫描上下文所在程序集。也可用 `services.TryAddRepository<TDbContext>(assemblies)` 扫描多个程序集。候选必须是公开、非抽象、非泛型的 `MongoEntity`；所有候选注册查询仓储，仅 `IAggregateRoot` 注册写仓储。使用 `TryAddScoped`，因此自定义仓储应先注册。

`AddMongoDbContext` 已注册 Mongo 专属弹性服务，不需要额外调用独立的 Mongo 弹性注册方法。通用 `ResilienceOptions` 仍由宿主现有配置决定重试次数和延迟。

## 6. 配置

```json
{
  "PracticeMongo": {
    "ConnectionString": "mongodb://mongo-host:27017/fantasy_practice",
    "DisableTracing": false,
    "MaxConnectionPoolSize": 100,
    "MinConnectionPoolSize": 0,
    "MaxConnectionIdleTime": "00:10:00",
    "MaxConnectionLifeTime": "00:30:00",
    "ConnectTimeout": "00:00:30",
    "SocketTimeout": "00:00:30",
    "ServerSelectionTimeout": "00:00:30",
    "EnablePerformanceMonitoring": true
  }
}
```

| `MongoOptions` | 默认值 | 说明 |
| --- | --- | --- |
| `ConnectionString` | 空字符串 | 必须包含数据库名；凭据由外部机密配置提供 |
| `DisableTracing` | `false` | `false` 时订阅 MongoDB Driver DiagnosticSource |
| `MaxConnectionPoolSize` | `100` | 必须大于 0 |
| `MinConnectionPoolSize` | `0` | 必须在 0 与最大值之间 |
| `MaxConnectionIdleTime` | 10 分钟 | 连接最大空闲时间 |
| `MaxConnectionLifeTime` | 30 分钟 | 连接最大生命周期 |
| `ConnectTimeout` | 30 秒 | 建立服务器连接超时 |
| `SocketTimeout` | 30 秒 | 套接字读写超时 |
| `ServerSelectionTimeout` | 30 秒 | 服务器选择超时 |
| `EnablePerformanceMonitoring` | `true` | 启用有界进程内仓储指标 |

客户端固定 `ReadConcern.Local` 和 `WriteConcern.WMajority`。如果业务需要不同一致性级别，应在评估事务、延迟与数据安全后扩展配置，而不是在单个仓储里悄悄覆盖。

## 7. BSON 全局约定与兼容性

首次注册任意 Mongo context 时，进程级全局执行一次：

- `DateTimeSerializationProvider`：DateTime 统一按 UTC 处理；
- Guid serializer：`GuidRepresentation.CSharpLegacy`；
- convention pack 名称：`BuildingBlocks.MongoDB`；
- 属性名转为 camelCase；
- 忽略文档中的额外字段；
- 枚举存为字符串；
- 不忽略默认值（`IgnoreIfDefault(false)`）。

这些设置对同进程内后续 MongoDB 类型生效，且不可按 context 撤销。改变 Guid 表示、字段命名或枚举表示会影响历史 BSON 的读取与索引，应通过独立数据迁移、兼容读取和回滚计划实施。

## 8. 最小可编译使用示例

```csharp
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Context;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Extensions;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using MongoDB.Driver;

public sealed class PracticeTask : MongoAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public sealed class PracticeDbContext(
    IMongoDatabase database,
    IMongoClient client) : MongoDbContext(database, client);

builder.AddMongoDbContext<PracticeDbContext>("PracticeMongo");
builder.AddMongoRepository<PracticeDbContext>();

public sealed class PracticeTaskService(
    IRepository<PracticeTask> repository,
    IQueryRepository<PracticeTask> queries,
    IMongoDbContext context)
{
    public async Task<PracticeTask> CreateAsync(string name, CancellationToken ct)
    {
        var task = new PracticeTask { Name = name };
        return await repository.InsertAsync(task, ct); // 已即时写入，无需 SaveChanges
    }

    public Task<PracticeTask?> GetAsync(string id, CancellationToken ct) =>
        queries.GetByIdAsync(id, ct);

    public async Task CompletePairAsync(PracticeTask first, PracticeTask second, CancellationToken ct)
    {
        await context.BeginTransactionAsync(ct);
        try
        {
            first.Status = second.Status = "Completed";
            await repository.UpdateAsync(first, ct);
            await repository.UpdateAsync(second, ct);
            await context.CommitTransactionAsync(ct);
        }
        catch
        {
            await context.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
```

MongoDB 多文档事务要求部署为副本集或分片集群；单机 standalone 环境会在开始/执行事务时失败。

## 9. 主流程与重试边界

### 事务外

每次仓储读取先创建性能计时器，再通过 `IMongoResilienceService` 执行。可重试判断包含连接错误、执行超时、普通超时，以及带 `RetryableWriteError`、`TransientTransactionError`、`UnknownTransactionCommitResult` 标签的 `MongoException`。但内置仓储只把**事务外读取**交给该服务。

写入不会做仓储级应用重试，只依赖驱动 retryable writes。业务仍需唯一索引、幂等键或事务，防止调用方重试造成重复副作用。

### 事务内

`BeginTransactionAsync` 创建 session 后，仓储所有读写都使用该 session。事务内不局部重试单条语句，因为重试其中一步可能重复部分副作用；应把“开始事务—全部读写—提交”作为完整单元重试，并根据 MongoDB 错误标签处理未知提交结果。

同一 session 不支持并行操作，所以分页在事务内先计数再取页；事务外则并行执行。上下文禁止嵌套活动事务，不创建保存点。

### 写入语义

MongoDB 没有 EF ChangeTracker。插入、替换、删除在方法完成前已发给数据库；`SaveChangesAsync` 仅为兼容统一仓储接口并固定返回 0，不能当作受影响文档数。`UpdateAsync` 是整文档替换，调用方必须避免用不完整对象覆盖未加载字段。

## 10. 取消、异常与安全语义

| 场景 | 行为 |
| --- | --- |
| 所有仓储异步方法 | `CancellationToken` 传给驱动及重试等待；取消向上传播 |
| `BeginTransactionAsync` 已有活动事务 | 抛 `InvalidOperationException` |
| 提交/回滚无活动事务 | no-op |
| 提交/回滚失败 | 异常向上传播，session 保留供收尾 |
| `UpdateAsync` 未匹配文档 | 抛 `InvalidOperationException` |
| `CountAsync` 超过 `int.MaxValue` | `checked` 转换抛 `OverflowException` |
| 页码/页大小非正 | 抛 `ArgumentOutOfRangeException` |
| ObjectId 字符串无效 | `EnsureValidMongoId` / 按 ID 查询扩展抛 `ArgumentException` |
| 连接字符串缺失、无数据库名、池边界无效 | 注册时抛 `InvalidOperationException` |
| Mongo 连接异常 | Problem Details 映射 503 |
| `TimeoutException` | 映射 408；`MongoExecutionTimeoutException` 若不派生该类型则由默认映射处理 |
| Mongo 写入/命令异常 | 映射 500；重复键当前也不会自动映射 409 |
| 健康检查非取消异常 | 记录 Warning 并返回 `false` |

不要在配置文件提交真实 MongoDB 凭据。集合名、筛选条件、分页上限和索引必须由业务控制，避免无界读取、集合名漂移和缺索引扫描。

## 11. 性能监控

启用时，每个仓储操作在内存中记录名称、集合、耗时和 UTC 完成时间；最多保留最近 10,000 条。超过 200ms 记录 Warning，超过 1s 记录 Error。`GetMetricsAsync()` 默认统计最近 5 分钟；`OperationsPerSecond` 使用所选完整时间窗口作分母。

关闭时 `StartOperation` 返回空句柄，不创建 `Stopwatch`。该监控没有持久化、跨实例聚合、百分位数、标签控制或导出器，不能替代 OpenTelemetry。

## 12. 已知限制与技术债

1. 默认集合名绑定 CLR 类型名，类型改名会隐式切换集合；生产聚合应使用显式稳定名称。
2. `GetListAsync()` 可无界加载；业务 API 必须限制查询规模。
3. `Query()` 暴露 Mongo LINQ，复杂表达式是否可翻译需由调用方验证。
4. 写仓储仅做整文档替换和物理删除，没有 patch、乐观并发或软删除。
5. `SaveChangesAsync` 固定返回 0，统一仓储抽象中的提交语义与 EF Core 不同。
6. 应用层重试只覆盖事务外读取；完整事务重试模板由业务实现。
7. MongoDB 事务依赖副本集/分片集群，组件不在启动时检测部署拓扑。
8. 全局 BSON 约定不可按 context 隔离；同进程引入其他 Mongo 模型时也会受影响。
9. Guid 使用 `CSharpLegacy` 是历史兼容约束，不应作为新系统的无条件推荐格式。
10. Problem Details 对重复键没有 409 专用映射，执行超时映射也需结合实际异常类型复核。
11. 性能指标只有最近 10,000 条进程内样本，没有分位数和导出。
12. 索引创建/迁移不由本组件管理，禁止捕获 scoped context 后用 fire-and-forget `Task.Run` 创建索引。

## 13. 测试与验证

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.MongoDB\BuildingBlocks.MongoDB.csproj --no-incremental
dotnet test src\BuildingBlocks\BuildingBlocks.Persistence.Tests\BuildingBlocks.Persistence.Tests.csproj
dotnet build src\Fantasy.slnx
dotnet test src\Fantasy.Tests.slnx
git diff --check
```

需要真实 MongoDB/Testcontainers 的测试依赖 Docker。被跳过的集成测试必须在交付结果中单列数量和原因。
