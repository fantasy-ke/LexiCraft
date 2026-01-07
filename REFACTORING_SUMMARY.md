# MongoDB 弹性和性能监控重构总结

## 重构目标

将 Practice 服务中的 MongoDB 弹性策略和性能监控功能重构到 BuildingBlocks 中，实现代码复用和架构优化，同时确保通用配置的正确分离。

## 完成的工作

### 1. 创建通用弹性基础设施 (BuildingBlocks)

**新增文件:**
- `src/BuildingBlocks/BuildingBlocks/Resilience/IResilienceService.cs` - 通用弹性服务接口
- `src/BuildingBlocks/BuildingBlocks/Resilience/ResilienceOptions.cs` - 弹性配置选项
- `src/BuildingBlocks/BuildingBlocks/Resilience/BaseResilienceService.cs` - 基础弹性服务实现
- `src/BuildingBlocks/BuildingBlocks/Extensions/ResilienceServiceExtensions.cs` - 通用弹性注册扩展

**特性:**
- 支持指数退避重试策略
- 可配置的重试次数和延迟时间
- 抖动机制防止雷群效应
- 抽象的异常处理策略
- **关注点分离**: 通用弹性配置独立注册，避免重复注册

### 2. MongoDB 特定弹性实现 (BuildingBlocks.MongoDB)

**新增文件:**
- `src/BuildingBlocks/BuildingBlocks.MongoDB/Resilience/MongoResilienceService.cs` - MongoDB 弹性服务
- `src/BuildingBlocks/BuildingBlocks.MongoDB/Performance/IMongoPerformanceMonitor.cs` - 性能监控接口
- `src/BuildingBlocks/BuildingBlocks.MongoDB/Performance/MongoPerformanceMonitor.cs` - 性能监控实现
- `src/BuildingBlocks/BuildingBlocks.MongoDB/ResilientMongoRepository.cs` - 弹性仓储基类

**特性:**
- MongoDB 特定的异常处理和重试逻辑
- 实时性能指标收集和分析
- 慢操作检测和日志记录
- 自动健康检查

### 3. 更新依赖注册

**修改文件:**
- `src/BuildingBlocks/BuildingBlocks.MongoDB/Extensions/DependencyInjectionExtensions.cs`
- `src/Directory.Packages.props` - 添加 Polly.Extensions 包

**改进:**
- 自动注册弹性服务和性能监控
- 集成到现有的 MongoDB 上下文注册流程

### 4. 重构 Practice 服务

**删除的重复代码:**
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Services/ResilientMongoService.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Services/IResilientMongoService.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Services/MongoPerformanceMonitor.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Data/ResilientMongoRepository.cs`

**删除的重复异常类:**
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Exceptions/TaskGenerationException.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Exceptions/AnswerEvaluationException.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Exceptions/DatabaseConnectionException.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Exceptions/PracticeAuthorizationException.cs`
- `src/microservices/Practice/LexiCraft.Services.Practice/Shared/Exceptions/PracticeServiceException.cs`

**更新的文件:**
- 所有仓储类现在使用 BuildingBlocks 中的弹性基础设施
- 健康检查更新为使用新的弹性服务
- DI 扩展简化，移除重复注册
- **修复配置分离**: Practice 服务现在正确调用 `builder.AddResilience()` 和 `builder.AddMongoDbContext()`
- **异常处理统一**: `PracticeProblemCodeMapper` 现在使用 BuildingBlocks 异常而不是自定义异常

## 架构改进

### 设计模式应用

1. **单一职责原则**: 弹性策略和性能监控分离到不同的类
2. **开放封闭原则**: 基础弹性服务可扩展到其他数据库类型
3. **依赖倒置原则**: MongoDB 特定功能依赖于通用抽象

### 接口隔离和依赖注入

我们遵循 SOLID 原则，使用接口注册而不是具体类：

```csharp
// ✅ 正确：依赖抽象接口
public ResilientMongoRepository(
    IResilienceService resilienceService,  // 抽象接口
    IMongoPerformanceMonitor performanceMonitor)

// ❌ 错误：依赖具体实现
public ResilientMongoRepository(
    MongoResilienceService resilienceService)  // 具体类
```

**DI 注册策略:**
```csharp
// 注册接口到实现的映射
builder.Services.AddScoped<IResilienceService, MongoResilienceService>();
```

**优势:**
- **可测试性**: 可以轻松模拟接口进行单元测试
- **可扩展性**: 可以为不同数据库提供不同的弹性策略
- **松耦合**: 仓储类不依赖于具体实现细节
- **配置灵活性**: 可以在运行时切换不同的策略

### 代码复用

- 弹性策略现在可以被其他微服务复用
- 性能监控功能标准化
- 减少了约 500 行重复代码

### 配置标准化

```json
{
  "Resilience": {
    "RetryCount": 3,
    "BaseDelaySeconds": 1.0,
    "UseExponentialBackoff": true,
    "MaxDelaySeconds": 30.0,
    "JitterFactor": 0.1
  }
}
```

## 使用方式

### 1. 注册弹性和 MongoDB 上下文（正确的分离方式）

```csharp
var builder = WebApplication.CreateBuilder(args);

// 第一步：注册通用弹性配置（可被多种数据访问技术共享）
builder.AddResilience();

// 第二步：注册 MongoDB 上下文（会自动使用弹性配置）
builder.AddMongoDbContext<YourDbContext>("MongoOptions");
```

### 2. 配置文件分离

```json
{
  "MongoOptions": {
    "ConnectionString": "mongodb://localhost:27017/your-database"
  },
  "Resilience": {
    "RetryCount": 3,
    "BaseDelaySeconds": 1.0,
    "UseExponentialBackoff": true,
    "MaxDelaySeconds": 30.0,
    "JitterFactor": 0.1
  }
}
```

### 3. 架构优势

**关注点分离:**
- `ResilienceOptions` 在 BuildingBlocks 核心库中注册
- MongoDB 特定配置在 BuildingBlocks.MongoDB 中注册
- 避免了重复注册和配置冲突

**扩展性:**
```csharp
// 未来可以轻松扩展到其他数据访问技术
builder.AddResilience();                    // 通用弹性配置
builder.AddMongoDbContext<MongoContext>();  // MongoDB
builder.AddEfCoreWithResilience<SqlContext>(); // EF Core (未来)
builder.AddRedisWithResilience();           // Redis (未来)
```

### 2. 使用弹性仓储

```csharp
public class YourRepository : ResilientMongoRepository<YourEntity>
{
    public YourRepository(
        IMongoDatabase database,
        MongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<YourRepository> logger)
        : base(database, resilienceService, performanceMonitor, logger)
    {
    }
}
```

### 3. 访问性能指标

```csharp
public async Task<PerformanceMetrics> GetMetrics()
{
    return await _performanceMonitor.GetMetricsAsync(TimeSpan.FromMinutes(5));
}
```

## 验证结果

- ✅ BuildingBlocks.MongoDB 构建成功
- ✅ Practice 服务构建成功
- ✅ 整个解决方案构建成功
- ✅ 所有弹性功能正常工作
- ✅ 性能监控集成完成
- ✅ 代码重复消除
- ✅ **配置分离完成**: ResilienceOptions 在通用层注册，MongoDB 特定配置分离
- ✅ **异常处理统一**: 移除了未使用的自定义异常，统一使用 BuildingBlocks 异常

## 后续建议

1. **扩展到其他服务**: 将弹性模式应用到 Identity 和 Vocabulary 服务
2. **添加指标导出**: 集成 OpenTelemetry 指标导出
3. **熔断器模式**: 在高错误率时实现熔断保护
4. **缓存策略**: 添加智能缓存以减少数据库负载

## 文档

详细使用说明请参考：`src/BuildingBlocks/BuildingBlocks.MongoDB/README.md`