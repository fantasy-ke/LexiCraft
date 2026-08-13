# BuildingBlocks.EntityFrameworkCore

该组件提供 EF Core 通用仓储、工作单元、审计拦截器和强类型 ID 转换器，不包含具体数据库提供程序。

## 注册

```csharp
services.WithDbAccess<FilesDbContext>(options =>
{
    options.UseSqlite(connectionString);
});
services.WithRepository<FilesDbContext>();
```

`WithDbAccess<TDbContext>` 会以 scoped 生命周期注册 `AuditableEntityInterceptor` 和 `IUnitOfWork`。仓储扫描默认只检查 `TDbContext` 所在程序集，避免加载全部引用程序集。

## 审计与软删除

- 新增实体：为未设置的 `Guid` / 强类型 `Guid` 主键生成 Guid v7，并写入 UTC 创建时间。
- 修改实体：写入 UTC 更新时间和当前用户信息。
- 删除实现 `ISoftDeleted` 的实体：仅把删除标记与删除审计属性标记为已修改，写入 `IsDeleted = true`、UTC 删除时间和删除人，不执行物理删除，也不覆盖脱离跟踪实体的其他字段。
- 删除未实现 `ISoftDeleted` 的实体：仍按 EF Core 的普通删除语义处理。

> `DeleteAsync(predicate)` 使用 `ExecuteDeleteAsync`，会绕过 ChangeTracker、`SaveChanges` 和审计拦截器，因此它始终是直接物理删除。需要软删除时应先加载实体，再调用实体删除方法并保存。

## 异步和分页

所有仓储与工作单元异步 API 均接受 `CancellationToken`。分页要求 `pageIndex > 0`、`pageSize > 0`，并检查 skip 乘法溢出。

```csharp
var (total, rows) = await repository.GetPageListAsync(
    x => x.IsActive,
    pageIndex: 1,
    pageSize: 20,
    cancellationToken: cancellationToken);
```

## 强类型 ID

`StrongIdValueConverter<TStrongId, TValue>` 在泛型类型初始化时编译一次构造委托，避免每次从数据库物化都调用反射构造。