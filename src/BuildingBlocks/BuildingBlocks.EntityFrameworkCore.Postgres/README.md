# BuildingBlocks.EntityFrameworkCore.Postgres

该组件是对 Entity Framework Core 的 Postgres 适配器封装，提供了蛇形命名、自动审计项填充、自动迁移与种子数据支持。

## 特性

- **蛇形命名**：自动将模型属性映射为数据库中的 `snake_case` 格式。
- **自动审计**：集成 `AuditableEntityInterceptor`，自动维护创建/修改时间。
- **自动迁移**：支持应用启动时自动执行 EF Core 迁移。
- **数据种子**：提供 `IDataSeeder` 接口，支持灵活的初始化数据填充。
- **存仓模式**：集成了通用 Repository 和 Unit of Work 模式。
- **时间规范**：默认开启 `Npgsql.EnableLegacyTimestampBehavior` 以兼容传统时间戳。

## 配置

在 `appsettings.json` 中配置：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=LexiCraft;Username=postgres;Password=password"
  },
  "PostgresOptions": {
    "UseInMemory": false,
    "MigrationAssembly": "Your.Project.Name"
  }
}
```

## 快速开始

### 1. 注册 DbContext

```csharp
builder.AddPostgresDbContext<LexiCraftDbContext>("DefaultConnection");
```

### 2. 启用自动迁移与种子数据

```csharp
// 自动执行迁移并支持自定义 SeedAsync
builder.AddMigration<LexiCraftDbContext>((context, sp) => 
{
    // 自定义种子逻辑
    return Task.CompletedTask;
});

// 或者使用专门的 Seeder 类
builder.AddMigration<LexiCraftDbContext, MyDataSeeder>();
```

### 3. 定义审计实体

继承 `AuditableEntity` (位于 `BuildingBlocks.Domain`)：

```csharp
public class Post : AuditableEntity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

### 4. 使用 Repository

```csharp
public class PostService(IRepository<Post> postRepository)
{
    public async Task AddPost(Post post)
    {
        await postRepository.AddAsync(post);
    }
}
```

## 核心 API

- `AddPostgresDbContext<TDbContext>`: 配置 Npgsql 数据源及审计拦截器。
- `AddMigration<TContext>`: 注册基于 `IHostedService` 的迁移工人。
- `UnitOfWork`: 支持跨仓储的事务提交。
