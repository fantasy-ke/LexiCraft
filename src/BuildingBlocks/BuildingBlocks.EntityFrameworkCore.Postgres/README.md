# BuildingBlocks.EntityFrameworkCore.Postgres

该组件在 `BuildingBlocks.EntityFrameworkCore` 之上提供 Npgsql、`snake_case` 命名、启动迁移和种子数据支持。

## 配置

连接字符串按以下顺序读取：

1. `AddPostgresDbContext` 传入的 `ConnectionStrings:<name>`；
2. `PostgresOptions:ConnectionString`。

```json
{
  "PostgresOptions": {
    "ConnectionString": "Host=localhost;Database=lexicraft;Username=postgres;Password=<secret>",
    "MigrationAssembly": "LexiCraft.Services.Identity"
  }
}
```

不要在仓库中提交真实凭据；示例值应通过环境变量、用户机密或部署平台密钥注入。

## 注册 DbContext

```csharp
builder.AddPostgresDbContext<IdentityDbContext>(nameof(PostgresOptions));
```

注册会：

- 启用 Npgsql 重试和 `snake_case`；
- 以 scoped 生命周期解析审计拦截器，不创建临时根容器；
- 仅扫描 `DbContext` 所在程序集注册默认仓储；
- 缺失连接字符串时立即失败。

## 迁移与种子数据

```csharp
builder.AddMigration<IdentityDbContext, IdentityDbDataSeeder>();
```

`IDataSeeder<TContext>.SeedAsync` 接受 `CancellationToken`。迁移工作器使用异步作用域，并在迁移或种子数据失败时让应用启动失败，不再仅记录日志后继续运行。

也可以使用委托：

```csharp
builder.AddMigration<IdentityDbContext>(async (db, services, cancellationToken) =>
{
    await SeedAsync(db, cancellationToken);
});
```

## 时间兼容

当前仍启用 `Npgsql.EnableLegacyTimestampBehavior` 与 `Npgsql.DisableDateTimeInfinityConversions`，用于兼容已有模型和数据。审计时间统一写入 UTC；移除旧时间开关前需要先核对现有 PostgreSQL 列类型与历史数据。