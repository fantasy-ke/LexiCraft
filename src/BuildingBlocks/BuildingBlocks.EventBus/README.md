# BuildingBlocks.EventBus

LexiCraft 的统一事件总线组件，现已正式集成至 **BuildingBlocks** 体系。

## 核心特性

- **混合分发**：支持无缝切换或同时使用 Local (Channel) 与 Distributed (Redis) 分发。
- **基座集成**：深度集成 `BuildingBlocks` 的 Options 扩展，实现代码与配置文件的双重驱动。
- **自动化配置**：支持从 `appsettings.json` 的 `EventBusOptions` 节点自动加载参数。
- **条件注册**：基于 `IHostApplicationBuilder` 扩展，根据配置文件开关按需注入服务。
- **Saga 支持**：内置 `CorrelationId` 传递逻辑，通过 `SagaExtensions` 简化分布式追踪。

## 快速配置

在微服务的 `appsettings.json` 中定义：

```json
{
  "EventBusOptions": {
    "EnableLocal": true,
    "EnableRedis": true,
    "Redis": {
      "ConnectionString": "127.0.0.1:6379,password=xxx",
      "Prefix": "lexi",
      "IdempotencyExpireSeconds": 86400
    }
  }
}
```

在 `Program.cs` 中一键注册：

```csharp
using BuildingBlocks.EventBus.Extensions;

// 默认绑定 appsettings 中的 "EventBusOptions" 节点
builder.AddZEventBus(); 
```

## 进阶使用案例

### 1. 配置自定义事件名称与 Channel

使用 `EventSchemeAttribute` 装饰事件类：

```csharp
using BuildingBlocks.EventBus.Shared;

// 使用主构造函数风格描述方案
[EventScheme("user.login.success", SingleReader = true)]
public record UserLoginSuccessEvent(Guid UserId, string UserName);
```

### 2. Saga 分布式事件追踪

通过 `SagaExtensions` 在多个集成事件间自动透传关键的 `CorrelationId`：

```csharp
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Shared;

// 1. 定义继承自 IntegrationEvent 的集成事件
public record OrderCreatedIntegrationEvent(Guid OrderId, decimal Amount) : IntegrationEvent;

public record ProcessPaymentIntegrationEvent(Guid CorrelationId, Guid OrderId) 
    : IntegrationEvent(Guid.NewGuid(), CorrelationId, DateTime.UtcNow);

// 2. 在处理器中生成并发布后续事件
public class OrderHandler : IEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IEventBus<ProcessPaymentIntegrationEvent> _eventBus;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct)
    {
        // 核心：使用 CreateNextEvent 保证 CorrelationId 链路完整性
        var nextEvent = @event.CreateNextEvent(correlationId => 
            new ProcessPaymentIntegrationEvent(correlationId, @event.OrderId));
            
        await _eventBus.PublishAsync(nextEvent);
    }
}
```

## 配置项详情 (EventBusOptions)

| 属性                               | 类型       | 默认值      | 说明                      |
|:---------------------------------|:---------|:---------|:------------------------|
| `EnableLocal`                    | `bool`   | `true`   | 是否开启本地内存消息分发            |
| `EnableRedis`                    | `bool`   | `false`  | 是否开启 Redis 发布订阅模式       |
| `Redis.ConnectionString`         | `string` | `null`   | Redis 连接串，启用 Redis 时必填  |
| `Redis.Prefix`                   | `string` | `"lexi"` | 频道名前缀（例如：lexi:TypeName） |
| `Redis.IdempotencyExpireSeconds` | `int`    | `86400`  | 原子幂等凭证的过期时长             |

## 目录结构说明

- `Abstractions/`: `IEventBus` 接口与 `IntegrationEvent` 基类。
- `Local/`: 高性能本地投递引擎。
- `Redis/`: 分布式消息分发及基于 `SetNx` + `Expire` 的消息防重逻辑。
- `Shared/`: 包含 `SagaExtensions` 链路追踪工具与 `EventSchemeAttribute` 特性。
- `Options/`: 类型安全的配置模型。
- `Extensions/`: `AddZEventBus` 驱动注册入口。
