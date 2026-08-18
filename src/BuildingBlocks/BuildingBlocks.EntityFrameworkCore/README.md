# BuildingBlocks.EntityFrameworkCore

## 用途与边界

该类库是 `BuildingBlocks.Persistence.Abstractions` 的 EF Core 适配器，提供：

- 通用仓储实现 `QueryRepository<TDbContext,TEntity>`（只读）与 `Repository<TDbContext,TEntity>`（聚合根读写）。
- 工作单元 `UnitOfWork<TDbContext>`：保存、显式事务、提供程序执行策略入口。
- 保存管线拦截器 `AuditableEntityInterceptor`：补全主键、写入创建/更新审计字段、把删除转换为软删除。
- 强类型 ID 支持：`StrongIdValueConverter<TStrongId,TValue>` 与 `ModelBuilder.ConfigureStrongIds()`。
- `IQueryable` 组合辅助：`WhereIf`、`PageBy`、`Page`、`QueryNoTracking`、`GetPageListAsync`。
- DI 注册扩展：`WithDbAccess`、`WithRepository`、`TryAddRepository`。
- 种子数据端口 `IDataSeeder<TContext>`（只有接口，执行流程不在本项目）。

它不提供：

- 具体数据库提供程序。`UseNpgsql` / `UseSqlite` 等由调用方在 `WithDbAccess` 的委托里指定。
- 连接字符串选项类、迁移执行器、设计时 `DbContext` 工厂、种子数据宿主。这些在 `BuildingBlocks.EntityFrameworkCore.Postgres`。
- 软删除的全局查询过滤器。拦截器只写入 `IsDeleted` 等字段，`HasQueryFilter` 必须由业务 `DbContext` 自行配置。
- 授权过滤、租户隔离、并发令牌、领域事件派发、outbox、审计历史表。

与抽象层的关系：`IQueryRepository<TEntity>`、`IRepository<TEntity>`、`IUnitOfWork` 定义在 `BuildingBlocks.Persistence.Abstractions`，本项目只实现它们。业务代码应注入抽象接口；只有需要 EF 专属扩展点（`DbContext`、`Entity`、`QuerySetNoTracking<T>()`）时才继承本项目的仓储基类。

## 依赖关系

项目引用：

| 引用 | 用途 |
| --- | --- |
| `BuildingBlocks` | `IEntity`、`IAggregateRoot`、`ICreatable`、`IUpdatable`、`ISoftDeleted`、`StrongId<TValue>`、`IUserContext` |
| `BuildingBlocks.Persistence.Abstractions` | 仓储与工作单元契约 |

NuGet 包（版本由仓库集中管理，`csproj` 内不写版本号）：

| 包 | 用途 |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | 上下文、ChangeTracker、保存拦截器基类 |
| `Microsoft.EntityFrameworkCore.Relational` | `ExecuteDeleteAsync`、事务与执行策略 API |
| `EFCore.NamingConventions` | 供上层提供程序包启用命名约定 |

`AuditableEntityInterceptor` 通过 `IdGen`（由 `BuildingBlocks` 传递引入）生成长整型 ID，`IdGenerator` 是可选依赖。

## 目录结构

```text
Abstractions/
  IDataSeeder.cs                 种子数据端口，SeedAsync(TContext, CancellationToken)
Converters/
  StrongIdValueConverter.cs      强类型 ID 与基础值之间的 EF 值转换器，构造委托只编译一次
Extensions/
  DependencyInjectionExtensions.cs  WithDbAccess / WithRepository / TryAddRepository
  ModelBuilderExtensions.cs      ConfigureStrongIds()，按模型反射注册强类型 ID 转换器
  QueryableExtensions.cs         WhereIf / PageBy / Page / QueryNoTracking / Count / GetPageListAsync
Interceptors/
  AuditableEntityInterceptor.cs  SaveChanges 前补主键、写审计字段、软删除转换
Repositories/
  QueryRepository.cs             只读仓储，默认跟踪查询
  Repository.cs                  在只读仓储之上增加写入，写入登记到 ChangeTracker
Transactions/
  UnitOfWork.cs                  SaveChanges、事务、执行策略、Dispose
```

## 公共 API 速查

`QueryRepository<TDbContext,TEntity> : IQueryRepository<TEntity>`（`TEntity : class`）：

| 成员 | 语义 |
| --- | --- |
| `TDbContext DbContext { get; }` | 公开当前作用域上下文，供派生仓储使用 |
| `protected DbSet<TEntity> Entity { get; }` | `DbContext.Set<TEntity>()` |
| `protected IQueryable<T> QuerySetNoTracking<T>()` | 同一上下文中其他实体的无跟踪查询，EF 专属扩展点 |
| `GetListAsync(predicate, ct)` / `GetListAsync(ct)` | 条件列表 / 全量列表，后者无分页保护 |
| `FirstOrDefaultAsync` / `FirstAsync` | 首条；`FirstAsync` 无结果时由 EF 抛 `InvalidOperationException` |
| `SingleOrDefaultAsync` / `SingleAsync` | 唯一匹配；多条（`SingleAsync` 还包括零条）时抛 `InvalidOperationException` |
| `CountAsync(predicate, ct)` / `AnyAsync(predicate, ct)` | 计数 / 存在性 |
| `GetAsync(predicate, ct)` | 语义等同 `FirstOrDefaultAsync`，不校验唯一性 |
| `Query()` / `QueryNoTracking()` | 跟踪 / 无跟踪 `IQueryable` 逃生口 |
| `GetPageListAsync(predicate, pageIndex, pageSize, orderBy, isAsc, ct)` | 先 `CountAsync` 再取当前页，返回 `(int total, IEnumerable<TEntity> result)` |

`Repository<TDbContext,TEntity> : QueryRepository<...>, IRepository<TEntity>`（`TEntity : class, IAggregateRoot`）：

| 成员 | 语义 | 写入时机 |
| --- | --- | --- |
| `InsertAsync(entity, ct)` | `DbSet.AddAsync`，返回被跟踪实体 | 延迟，等待保存 |
| `InsertAsync(entities, ct)` | `DbSet.AddRangeAsync` | 延迟，等待保存 |
| `UpdateAsync(entity, ct)` | `DbSet.Update`，同步执行，仅先检查取消 | 延迟，等待保存 |
| `DeleteAsync(entity, ct)` | `DbSet.Remove`，同步执行，仅先检查取消 | 延迟，可被拦截器转为软删除 |
| `DeleteAsync(predicate, ct)` | `Where(predicate).ExecuteDeleteAsync` | **即时物理删除**，绕过 ChangeTracker 与拦截器 |
| `SaveChangesAsync(ct)` | `DbContext.SaveChangesAsync` | 提交当前上下文全部待写变更 |

`UnitOfWork<TDbContext> : IUnitOfWork`：

| 成员 | 语义 |
| --- | --- |
| `BeginTransactionAsync(ct)` | `Database.BeginTransactionAsync`，不做嵌套计数，也不创建保存点 |
| `CommitTransactionAsync(ct)` / `RollbackTransactionAsync(ct)` | `Database.CommitTransactionAsync` / `RollbackTransactionAsync` |
| `SaveChangesAsync(ct)` | `DbContext.SaveChangesAsync`，返回写入的状态条目数 |
| `ExecuteAsync(Func<Task>, ct)` / `ExecuteAsync<TResult>(Func<Task<TResult>>, ct)` | `Database.CreateExecutionStrategy().ExecuteAsync(...)`；不自动开事务、不自动保存 |
| `Dispose()` / `DisposeAsync()` | 释放注入进来的 `DbContext` |

`AuditableEntityInterceptor(IUserContext? userContext = null, IdGenerator? idGenerator = null) : SaveChangesInterceptor`：重写 `SavingChanges` 与 `SavingChangesAsync`，两条路径共用同一套实体处理逻辑。

扩展方法：

| 签名 | 语义 |
| --- | --- |
| `IServiceCollection WithDbAccess<TDbContext>(this IServiceCollection, Action<DbContextOptionsBuilder>)` | 注册上下文、审计拦截器、`IUnitOfWork` |
| `IServiceCollection WithRepository<TDbContext>(this IServiceCollection)` | 扫描 `typeof(TDbContext).Assembly` 注册默认仓储 |
| `IServiceCollection TryAddRepository<TDbContext>(this IServiceCollection, IEnumerable<Assembly>)` | 指定程序集扫描，程序集去重，`TryAdd` 语义 |
| `void ConfigureStrongIds(this ModelBuilder)` | 为模型中实现 `IStrongId` 的公开属性注册值转换器 |
| `IQueryable<T> PageBy<T>(this IQueryable<T>, int skipCount, int maxResultCount)` | `Skip` + `Take` |
| `TQueryable PageBy<T,TQueryable>(this TQueryable, int, int)` | 保留调用方查询类型，转换失败抛 `InvalidCastException` |
| `IQueryable<T> WhereIf<T>(this IQueryable<T>, bool, Expression<Func<T,bool>>)` | 条件成立才附加筛选；另有带索引谓词与保留查询类型的三个重载 |
| `IQueryable<T> Count<T>(this IQueryable<T>, out long count)` | **同步**计数并返回原查询 |
| `IQueryable<T> QueryNoTracking<T>(this IQueryable<T>)` | `AsNoTracking()` |
| `IQueryable<T> Page<T>(this IQueryable<T>, int pageNumber, int pageSize)` | `pageNumber < 1` 按第 1 页处理，不校验 `pageSize`，不加排序 |
| `Task<(int total, IEnumerable<T> result)> GetPageListAsync<T>(this IQueryable<T>, predicate, pageIndex, pageSize, orderBy, isAsc, ct)` | 与仓储同名方法同语义的 `IQueryable` 版本 |

## 注册顺序与扩展方法

```csharp
// 1) 可选前置：审计字段需要的上下文与 ID 生成器
services.AddScoped<IUserContext, UserContext>();   // 通常由 BuildingBlocks.Authorization 注册
services.AddIdGen(123, () => new IdGeneratorOptions()); // 仅当存在 long / IStrongId<long> 主键时需要

// 2) 可选前置：自定义仓储要在扫描之前注册，才能被 TryAdd 保留
services.AddScoped<IRepository<Spell>, SpellRepository>();

// 3) 上下文 + 拦截器 + 工作单元
services.WithDbAccess<CatalogDbContext>(options => options.UseSqlite(connectionString));

// 4) 默认仓储扫描
services.WithRepository<CatalogDbContext>();
```

`WithDbAccess<TDbContext>` 实际注册：

- `TryAddScoped<AuditableEntityInterceptor>()`；
- `AddDbContext<TDbContext>((sp, options) => { optionsAction(options); options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()); })`，上下文为 scoped，拦截器从同一作用域解析，不创建临时根容器；
- `AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>()`。

`WithRepository<TDbContext>` / `TryAddRepository<TDbContext>` 的扫描规则：

- 候选类型必须是导出的、`IsClass`、非泛型、非抽象且实现 `IEntity`；
- 每个候选类型注册 `IQueryRepository<TEntity>` → `QueryRepository<TDbContext,TEntity>`；
- 只有同时实现 `IAggregateRoot` 的类型才注册 `IRepository<TEntity>` → `Repository<TDbContext,TEntity>`；
- 全部使用 `TryAddScoped`，因此调用顺序决定自定义实现是否被保留；
- 只扫描传入的程序集。实体分布在多个程序集时必须显式调用 `TryAddRepository` 并传入全部程序集。

必须的调用顺序与前置条件：

1. `WithDbAccess` 必须在解析 `IUnitOfWork` 或任何仓储之前调用，否则 `IUnitOfWork` 缺失。
2. 自定义仓储注册要早于 `WithRepository`。
3. `modelBuilder.ConfigureStrongIds()` 必须在实体（含 `ApplyConfigurationsFromAssembly`）已加入模型之后调用，它遍历的是 `modelBuilder.Model.GetEntityTypes()`。
4. `IUserContext` 与 `IdGenerator` 是可选构造参数；不注册时不会启动失败，只是审计字段回退（见配置表）。

上层提供程序包提供组合入口：`AddPostgresDbContext<TDbContext>` 内部完成同样的拦截器、`IUnitOfWork` 与 `WithRepository` 注册，此时不需要再调用 `WithDbAccess`。

## 配置表

本项目没有自己的选项类，`WithDbAccess` 不读取任何配置节。连接字符串与提供程序选项完全由调用方在 `optionsAction` 中决定；带选项类的注册入口是 `BuildingBlocks.EntityFrameworkCore.Postgres` 的 `PostgresOptions`。

真正影响运行时行为的输入如下（默认值取自源码）：

| 输入 | 来源 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `optionsAction` | `WithDbAccess` 参数 | 无 | 必须显式选择提供程序与连接字符串，缺失会在首次解析上下文时失败 |
| `IUserContext` | DI，可选 | 未注册 | 缺失时 `CreateByName` / `UpdateByName` 回退为 `systemUser`，`DeleteByName` 保持 `null`，`CreateById` / `UpdateById` / `DeleteById` 不写入 |
| `IdGenerator` | DI，可选 | 未注册 | 缺失时 `long` 与 `IStrongId<long>` 主键不会自动生成，需要业务自行赋值 |
| `pageIndex` | 分页参数 | 无 | 必须 ≥ 1，否则 `ArgumentOutOfRangeException` |
| `pageSize` | 分页参数 | 无 | 必须 ≥ 1，否则 `ArgumentOutOfRangeException` |
| `orderBy` | 分页参数 | `null` | 为空时不追加排序，页顺序不保证稳定 |
| `isAsc` | 分页参数 | `true` | `false` 时使用 `OrderByDescending` |
| `Page(pageNumber, pageSize)` | 查询扩展参数 | 无 | `pageNumber < 1` 按第 1 页处理，不校验 `pageSize` |

对应的 `appsettings.json` 片段由调用方定义，本项目只消费传入的字符串。示例采用调用方自有的 `ConnectionStrings` 键：

```json
{
  "ConnectionStrings": {
    "Catalog": "Data Source=catalog.db"
  }
}
```

```csharp
services.WithDbAccess<CatalogDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("Catalog")));
```

生产环境不要把真实凭据写入 `appsettings.json`，使用环境变量、用户机密或部署平台密钥管理。

## 最小可编译使用示例

```csharp
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// 聚合根：继承审计基类即可获得创建/更新/软删除字段
public sealed class Spell : AuditAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Spell> Spells => Set<Spell>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 软删除查询过滤器不由基础组件提供，需要业务上下文自行声明
        modelBuilder.Entity<Spell>().HasQueryFilter(x => !x.IsDeleted);

        // 强类型 ID 转换必须在实体加入模型之后配置
        modelBuilder.ConfigureStrongIds();
    }
}

public sealed class SpellService(
    IRepository<Spell> repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Spell> CreateAsync(string name, int level, CancellationToken cancellationToken)
    {
        var spell = new Spell { Name = name, Level = level };

        // 仓储只登记变更，此处还没有任何 SQL 发出
        await repository.InsertAsync(spell, cancellationToken);

        // 统一提交点：审计拦截器在这里补全 Id、CreateAt、CreateByName
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return spell;
    }

    public Task<(int total, IEnumerable<Spell> result)> PageAsync(int page, CancellationToken cancellationToken) =>
        repository.GetPageListAsync(
            x => x.Level > 0,
            pageIndex: page,
            pageSize: 20,
            orderBy: x => x.Name,
            cancellationToken: cancellationToken);
}

// 组合根（例如 Fantasy.Services.Identity 的存储扩展）
public static class CatalogStorageExtensions
{
    public static IServiceCollection AddCatalogStorage(this IServiceCollection services, string connectionString)
    {
        services.WithDbAccess<CatalogDbContext>(options => options.UseSqlite(connectionString));
        services.WithRepository<CatalogDbContext>();
        return services;
    }
}
```

## 主流程说明

### 写入：即时写 vs 延迟写

`Repository` 的单实体方法只改变 `ChangeTracker` 状态，不发送任何命令：

1. `InsertAsync` → `Added`；`UpdateAsync` → `Modified`；`DeleteAsync(entity)` → `Deleted`。
2. `IUnitOfWork.SaveChangesAsync`（或 `Repository.SaveChangesAsync`，两者都调用同一个作用域 `DbContext`）触发保存。
3. EF Core 保存管线调用 `AuditableEntityInterceptor.SavingChangesAsync`，遍历 `ChangeTracker.Entries<IEntity>()` 补全字段并把软删除条目改回 `Unchanged`。
4. EF 生成并执行 SQL，返回受影响的状态条目数。

唯一的例外是 `DeleteAsync(predicate)`：它走 `ExecuteDeleteAsync`，命令在方法内立即执行，不经过 ChangeTracker，也不触发拦截器，因此永远是物理删除，且不会更新已加载实体的内存状态。

同一作用域内的 `Repository<TDbContext,TEntity>`、`QueryRepository<TDbContext,TEntity>` 和 `UnitOfWork<TDbContext>` 共享同一个 `DbContext` 实例，所以任意一个仓储登记的变更都会被一次 `SaveChangesAsync` 一起提交。这也意味着不同聚合的写入没有隔离，业务需要自己控制提交边界。

### 事务边界

```csharp
await unitOfWork.ExecuteAsync(async () =>
{
    await unitOfWork.BeginTransactionAsync(cancellationToken);
    try
    {
        await repository.InsertAsync(spell, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitTransactionAsync(cancellationToken);
    }
    catch
    {
        await unitOfWork.RollbackTransactionAsync(cancellationToken);
        throw;
    }
}, cancellationToken);
```

- `ExecuteAsync` 只包裹提供程序执行策略，不会自动开启事务、保存或提交。启用了 `EnableRetryOnFailure` 的提供程序要求"开始事务—写入—保存—提交"整体位于同一个执行策略委托内，否则重试会失败。
- 委托可能被执行多次，闭包内不得包含没有幂等保护的外部副作用。
- 本类库不维护事务嵌套计数，也不创建保存点：`BeginTransactionAsync` 直接转发给 `DatabaseFacade`，在同一上下文已有活动事务时由 EF Core 决定行为（会抛异常，不会静默复用）。需要"嵌套"语义时应由业务层保证只有最外层开启事务。
- `CommitTransactionAsync` / `RollbackTransactionAsync` 在没有活动事务时同样直接转发，由 EF Core 抛出异常。
- 事务生命周期绑定 `DbContext` 生命周期。`DbContext` 由 `AddDbContext` 注册为 scoped，请求结束时随作用域释放；未提交的事务会随之回滚。

### 审计、软删除与物理删除

`AuditableEntityInterceptor` 在同步/异步 `SaveChanges` 的保存前阶段运行：

- `Added`：为 `Guid` / `IStrongId<Guid>` 空主键生成 UUID v7；注入 `IdGenerator` 时为 `long` / `IStrongId<long>` 非正主键生成值；`CreateAt` 为空时写 UTC；创建用户名为空时写当前用户名或 `systemUser`。
- `Modified`：覆盖 `UpdateAt` 为当前 UTC；更新用户名为空时写当前用户名或 `systemUser`。已有审计用户名不会被覆盖。
- `Deleted` 且实体实现 `ISoftDeleted`：把条目改为 `Unchanged`，只标记存在于模型中的删除标记与删除审计字段为 Modified，降低 detached entity 覆盖其他列的风险。
- `Deleted` 但未实现 `ISoftDeleted`：保持 `Deleted`，由 EF 执行物理删除。

拦截器不会自动添加 `HasQueryFilter`。业务模型必须显式配置 `HasQueryFilter(x => !x.IsDeleted)`；否则软删除记录仍会被普通查询读到。`ExecuteDeleteAsync` 直接发 SQL，始终绕过拦截器、审计与查询跟踪。

### 强类型 ID

`ConfigureStrongIds()` 遍历已加入 EF 模型的实体 CLR 类型公开属性，仅处理实现 `IStrongId` 的属性。它反射基础值类型，闭合 `StrongIdValueConverter<TStrongId,TValue>` 并注册转换。强类型 ID 必须：

1. 派生自 `StrongId<TValue>`；
2. 公开一个只接收 `TValue` 的构造函数；
3. 在实体加入模型后调用 `ConfigureStrongIds()`。

转换器按闭合泛型类型缓存编译后的构造委托。它不自动配置 `ValueComparer`、数据库列类型或值生成策略；可变强类型 ID 或提供程序特殊类型需要业务额外配置。

### 查询组合与分页

`WhereIf` 只在条件成立时添加谓词。`PageBy` / `Page` 只做 `Skip` / `Take`，不会自动排序；稳定分页必须由调用方先添加唯一且稳定的排序。仓储 `GetPageListAsync` 会校验页码与页大小、先计数再取页；两条查询之间可能发生并发写入，因此总数与当前页不是同一快照，除非业务显式使用适当事务隔离级别。

## 取消、异常与安全语义

| 场景 | 行为 |
| --- | --- |
| 仓储异步查询/写入/保存 | `CancellationToken` 传给 EF Core；取消通常传播为 `OperationCanceledException` |
| `UpdateAsync(entity)` / `DeleteAsync(entity)` | 方法本身只先检查取消，再同步修改 ChangeTracker |
| `FirstAsync` 无结果 | EF Core 抛 `InvalidOperationException` |
| `SingleAsync` 零条或多条、`SingleOrDefaultAsync` 多条 | EF Core 抛 `InvalidOperationException` |
| 页码或页大小 ≤ 0 | 仓储分页抛 `ArgumentOutOfRangeException` |
| 分页算术溢出 | `checked` 计算抛 `OverflowException` |
| 强类型 ID 缺少基础值构造函数 | 创建转换器时抛 `InvalidOperationException` |
| 同一上下文嵌套 `BeginTransactionAsync` | 本项目不复用事务或建保存点，由 EF Core 抛出提供程序异常 |
| 无活动事务时提交/回滚 | 直接转发 `DatabaseFacade`，由 EF Core 抛出异常 |
| `ExecuteDeleteAsync` | 即时物理删除；绕过软删除、审计、ChangeTracker 和 `SaveChanges` |
| 释放 `UnitOfWork` | 会释放注入的 scoped `DbContext`；释放后不可继续使用同作用域仓储 |

数据库异常不在本类库映射或隐藏；应由宿主异常处理与具体提供程序适配器统一转换。查询表达式、动态排序和分页边界必须由业务限制，避免无界全表加载和不稳定分页。

## 已知限制与技术债

1. `GetListAsync()` 提供无条件全量读取，调用方必须自行保证数据规模有界。
2. 分页在没有 `orderBy` 时不保证顺序稳定；计数与取页也不是原子快照。
3. `Page(pageNumber, pageSize)` 把非正页码归一为 1，但不校验 `pageSize`，与仓储分页规则不完全一致。
4. `Count(out long)` 是同步数据库查询，不适合异步请求热路径。
5. 软删除查询过滤器由每个业务上下文重复配置，基础组件不验证是否遗漏。
6. `ExecuteDeleteAsync` 的物理删除语义与实体删除重载明显不同，调用方必须显式审查。
7. `UnitOfWork` 不支持事务嵌套、保存点、自动保存/提交或自动回滚模板。
8. `Dispose` / `DisposeAsync` 主动释放注入的 `DbContext`；同作用域重复使用其他依赖时需避免提前释放工作单元。
9. 强类型 ID 自动配置不处理非公开属性、shadow property、可变值比较和提供程序专属映射。
10. 审计只记录当前值，不提供审计历史表、outbox 或领域事件派发。

## 测试与验证

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.EntityFrameworkCore\BuildingBlocks.EntityFrameworkCore.csproj --no-incremental
dotnet test src\BuildingBlocks\BuildingBlocks.Persistence.Tests\BuildingBlocks.Persistence.Tests.csproj
dotnet build src\Fantasy.slnx
dotnet test src\Fantasy.Tests.slnx
git diff --check
```

持久化测试包含 SQLite 行为验证与 Postgres/Testcontainers 场景；需要 Docker 的测试若被跳过，应检查跳过数量和原因，不能把退出码单独当作集成覆盖证明。
