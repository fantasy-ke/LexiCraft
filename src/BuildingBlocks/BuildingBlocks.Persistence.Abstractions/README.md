# BuildingBlocks.Persistence.Abstractions

## 用途与边界

该类库只定义持久化端口，不引用 EF Core、Npgsql 或 MongoDB 驱动。业务层可以依赖这些接口表达查询、聚合写入和工作单元，而不把具体数据库包带入应用核心。

它不负责数据库连接、对象映射、迁移、重试、审计、软删除实现或依赖注入注册。具体适配器位于 `BuildingBlocks.EntityFrameworkCore`、`BuildingBlocks.EntityFrameworkCore.Postgres` 和 `BuildingBlocks.MongoDB`。

## 依赖与目录

项目仅引用 `BuildingBlocks`，用于取得领域层的 `IAggregateRoot` 标记。

```text
Repositories/
  IQueryRepository.cs   实体读取、计数、存在性、分页与 IQueryable 入口
  IRepository.cs        聚合根添加、更新、删除与兼容保存入口
Transactions/
  IUnitOfWork.cs        EF 等延迟写提供程序的保存、事务和执行策略边界
```

业务项目若在构造函数或自定义仓储接口中直接使用这些端口，应显式引用本项目，不依赖适配器的传递项目引用。

## 公共 API

| API | 语义 |
| --- | --- |
| `IQueryRepository<TEntity>` | 同一实体类型的查询契约；表达式翻译由提供程序决定 |
| `Query()` | 高级查询逃生口；EF 返回跟踪查询，Mongo 返回 LINQ 查询 |
| `QueryNoTracking()` | EF 禁用跟踪；Mongo 与 `Query()` 等价 |
| `GetPageListAsync(...)` | 页码从 1 开始，同时返回筛选总数和当前页 |
| `IRepository<TEntity>` | 仅限 `IAggregateRoot` 的写入契约 |
| `DeleteAsync(entity)` | EF 可经保存拦截器软删除；Mongo 内置实现物理删除 |
| `DeleteAsync(predicate)` | 两种内置适配器都直接物理删除 |
| `SaveChangesAsync()` | EF 提交 ChangeTracker；Mongo 写入已即时生效并固定返回 `0` |
| `IUnitOfWork` | 当前内置实现用于 EF 保存、显式事务及提供程序执行策略 |

## 可编译用例

以下代码只依赖抽象项目和领域标记，可放入业务类库：

```csharp
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.Persistence.Abstractions.Transactions;

public sealed class SpellBook : IAggregateRoot
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
}

public sealed class SpellBookService(
    IQueryRepository<SpellBook> queries,
    IRepository<SpellBook> repository,
    IUnitOfWork unitOfWork)
{
    public Task<(int total, IEnumerable<SpellBook> result)> ListAsync(
        int page,
        CancellationToken cancellationToken) =>
        queries.GetPageListAsync(
            x => x.Title != string.Empty,
            page,
            pageSize: 20,
            orderBy: x => x.Title,
            cancellationToken: cancellationToken);

    public async Task<SpellBook> CreateAsync(
        SpellBook spellBook,
        CancellationToken cancellationToken)
    {
        await repository.InsertAsync(spellBook, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return spellBook;
    }
}
```

运行时必须注册与所选数据库匹配的实现。若使用 MongoDB，通常不应注入当前 EF 专属的 `IUnitOfWork`，且创建方法中的统一保存调用没有必要。

## 提供程序差异

| 行为 | EF Core | MongoDB |
| --- | --- | --- |
| 写入时机 | 先变更跟踪状态，保存时提交 | 仓储方法内立即发送命令 |
| 保存返回值 | 写入数据库的状态条目数 | 固定为 `0` |
| 实体删除 | 支持由拦截器转换为软删除 | 内置仓储物理删除 |
| 谓词删除 | 直接物理删除，绕过拦截器 | 直接批量物理删除 |
| 跟踪查询 | 默认跟踪，可显式禁用 | 无 EF 式跟踪 |
| 事务入口 | `IUnitOfWork` | 使用 Mongo 上下文 session API |

## 分页、取消与异常

- `pageIndex` 和 `pageSize` 必须大于 0；偏移量或 Mongo 计数超过 `int` 范围时会抛出 `OverflowException`。
- 未提供 `orderBy` 时，数据库不保证跨请求的稳定页顺序；游标分页或稳定分页必须使用唯一、确定的排序键。
- 所有异步方法都接受 `CancellationToken`。业务层应把请求或宿主令牌原样传入，不能用 `CancellationToken.None` 截断取消。
- `FirstAsync` 在没有结果时抛出，`SingleAsync` 在零条或多条时抛出；不确定存在性时使用可空重载。
- `Query()` 暴露提供程序表达式翻译边界。不要假设任意 CLR 方法都能被 SQL 或 Mongo LINQ 翻译。

## 安全与限制

- 抽象接口不执行授权、租户隔离或数据范围过滤；这些约束必须由业务查询或具体仓储保证。
- `GetListAsync()` 无分页上限，不应用于不受控的大表或大集合。
- 直接物理删除不可由审计拦截器恢复；调用前应显式确认数据保留策略。
- 执行策略可能多次调用委托，不应在委托中执行没有幂等或事务保护的外部副作用。

## 构建与测试

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.Persistence.Abstractions\BuildingBlocks.Persistence.Abstractions.csproj --no-restore
dotnet test src\Fantasy.Tests.slnx --no-restore
dotnet build src\Fantasy.slnx --no-restore
```

项目启用 `GenerateDocumentationFile`；新增公开 API 应同时提供有意义的 XML 文档并保持构建无 CS1591。
