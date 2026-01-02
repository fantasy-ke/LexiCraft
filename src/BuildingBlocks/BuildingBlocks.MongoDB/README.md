# BuildingBlocks.MongoDB

该组件提供了基于 MongoDB Driver 的基础封装，支持 `DbContext` 模式、仓储模式、对象序列化配置以及 OpenTelemetry 链路追踪。

## 特性

- **仓储模式**：提供统一的 `IRepository<T>` 接口实现，支持基础的 CRUD 操作。
- **DbContext 支持**：通过 `MongoDbContext` 管理连接和 Database。
- **自动注册**：支持扫描程序集并自动注册实体仓储。
- **链路追踪**：集成 `DiagnosticsActivityEventSubscriber`，支持分布式链路追踪。
- **序列化增强**：内置 `DateTimeSerializationProvider` 解决日期序列化时区问题。

## 配置

在 `appsettings.json` 中添加如下配置：

```json
{
  "MongoOptions": {
    "ConnectionString": "mongodb://localhost:27017/LexiCraft",
    "DisableTracing": false
  }
}
```

## 快速开始

### 1. 注册服务

在 `Program.cs` 中添加：

```csharp
builder.AddMongoDbContext<MyDbContext>("MongoOptions");
builder.AddMongoRepository<MyDbContext>();
```

### 2. 定义实体

实体需继承 `MongoEntity`：

```csharp
public class User : MongoEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### 3. 定义 DbContext

```csharp
public class MyDbContext(IMongoDatabase database) : MongoDbContext(database)
{
    // 可以定义 Collection 名称
}
```

### 4. 使用仓储

```csharp
public class UserService(IRepository<User> userRepository)
{
    public async Task CreateUserAsync(User user)
    {
        await userRepository.AddAsync(user);
    }

    public async Task<User?> GetUserAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }
}
```

## 核心 API

### IRepository<T>

- `ListAsync(Expression<Func<T, bool>> predicate)`
- `GetByIdAsync(Guid id)`
- `AddAsync(T entity)`
- `UpdateAsync(T entity)`
- `DeleteAsync(T entity)`
- `CountAsync(Expression<Func<T, bool>> predicate)`
