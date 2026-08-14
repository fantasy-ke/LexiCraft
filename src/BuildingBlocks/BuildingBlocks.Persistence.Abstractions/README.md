# BuildingBlocks.Persistence.Abstractions

该类库只定义持久化端口，不引用 EF Core、Npgsql 或 MongoDB 驱动。

## 目录职责

- `Repositories/IQueryRepository<TEntity>`：同一实体类型的读取、分页与高级查询入口。
- `Repositories/IRepository<TEntity>`：聚合根写入契约。
- `Transactions/IUnitOfWork`：延迟写提供程序的保存与事务边界。

该项目仅引用 `BuildingBlocks` 取得领域层的 `IAggregateRoot` 标记。具体实现分别位于：

- `BuildingBlocks.EntityFrameworkCore`
- `BuildingBlocks.MongoDB`

业务项目应直接引用本项目声明仓储契约，不依赖提供程序项目的传递引用。

## 边界说明

`Query()` / `QueryNoTracking()` 是保留的高级查询逃生口，表达式翻译能力仍由实际提供程序决定。公共接口不再提供跨实体泛型查询，因为 MongoDB 无法诚实实现该能力。

`IRepository<TEntity>.SaveChangesAsync()` 暂时保留用于兼容已有 EF 调用链。EF 会提交 ChangeTracker；MongoDB 写入即时生效并返回 `0`。新增业务优先通过 `IUnitOfWork` 表达 EF 的统一提交边界。