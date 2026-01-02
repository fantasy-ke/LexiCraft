# BuildingBlocks.EventBus

LexiCraft 的统一事件总线组件，现已正式集成至 **BuildingBlocks** 体系。

## 核心特性

- **混合分发**：支持无缝切换或同时使用 Local (Channel) 与 Distributed (Redis) 分发。
- **基座集成**：深度集成 `BuildingBlocks` 的 Options 扩展，实现代码与配置文件的双重驱动。
- **自动化配置**：支持从 `appsettings.json` 的 `EventBusOptions` 节点自动加载参数。
- **条件注册**：根据开关自动控制服务注入，避免不必要的资源开销。
- **Saga 支持**：内置 `CorrelationId` 传递逻辑，支持分布式事务追踪。

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
使用 `EventSchemeAttribute` 标记事件：

```csharp
using BuildingBlocks.EventBus.Shared;

[EventScheme("user.login.success", SingleReader = true)]
public record UserLoginSuccessEvent(Guid UserId, string UserName);
```

### 2. Saga 分布式事件追踪
继承 `IntegrationEvent` 并在流程中传递 `CorrelationId`：

```csharp
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Shared;

// 定义集成事件
public record OrderCreatedIntegrationEvent(Guid OrderId, decimal Amount) : IntegrationEvent;

public record ProcessPaymentIntegrationEvent(Guid CorrelationId, Guid OrderId) : IntegrationEvent(Guid.NewGuid(), CorrelationId, DateTime.UtcNow);

// 在处理器中生成后续事件
public class OrderHandler : IEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IEventBus<ProcessPaymentIntegrationEvent> _eventBus;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct)
    {
        // 使用 SagaExtensions 自动传递关键的 CorrelationId
        var nextEvent = @event.CreateNextEvent(correlationId => 
            new ProcessPaymentIntegrationEvent(correlationId, @event.OrderId));
            
        await _eventBus.PublishAsync(nextEvent);
    }
}
```

## 配置项详情 (EventBusOptions)

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `EnableLocal` | `bool` | `true` | 是否在当前进程内通过 Channel 分发消息 |
| `EnableRedis` | `bool` | `false` | 是否开启 Redis 发布订阅模式用于集成事件 |
| `Redis.ConnectionString`| `string` | `null` | Redis 连接串，启用 Redis 时必填 |
| `Redis.Prefix` | `string` | `"lexi"` | Redis 订阅频道名前缀 |
| `Redis.IdempotencyExpireSeconds` | `int` | `86400` | 幂等性 Key 的过期秒数 |

## 目录结构说明

- `Abstractions/`: 定义 `IEventBus`, `IEventHandler`, `IntegrationEvent` 等核心模型。
- `Local/`: 基于高性能内存队列 (Channels) 的本地实现。
- `Redis/`: 包含分布式分发、消费者后台服务及基于 Redis 的原子幂等性(`SetNx`)防重。
- `Shared/`: 序列化组件、Saga 扩展方法及 `EventSchemeAttribute`。
- `Options/`: 配置绑定模型。
- `Extensions/`: 统合注册入口。
