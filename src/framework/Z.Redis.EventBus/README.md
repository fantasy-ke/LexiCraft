# Z.Redis.EventBus

基于 Redis Pub/Sub 实现的轻量级分布式事件总线，专门为微服务架构下的跨服务通信和 **Saga 分布式事务** 设计。

## 核心特性

- **跨服务通信**：利用 Redis 实现进程间的异步解耦。
- **Saga 支持**：原生支持 `CorrelationId` 传递，轻松追踪跨服务业务流。
- **自动订阅**：后台服务自动发现并订阅实现了 `IEventHandler<T>` 且类型为集成事件的处理程序。
- **内置幂等性**：集成基于 Redis 的占位校验机制，利用 `IntegrationEvent.Id` 自动防止消息重复消费。
- **物理隔离**：通过接口约束，自动隔离本地事件与分布式集成事件。

## 快速开始

### 1. 注册服务

在各微服务的 `Program.cs` 中添加 Redis 事件总线支持：

```csharp
using Z.Redis.EventBus;

// 方式 A：自动从 DI 获取已存在的 RedisClient
builder.Services.AddRedisEventBus();

// 方式 B：手动指定连接字符串
builder.Services.AddRedisEventBus("127.0.0.1:6379,password=xxx");
```

### 2. 定义集成事件

必须继承 `IntegrationEvent` 基类或实现 `ISagaIntegrationEvent` 接口：

```csharp
using Z.EventBus;

public record OrderCreatedIntegrationEvent(
    Guid OrderId, 
    decimal Amount, 
    string BuyerId) : IntegrationEvent;
```

### 3. 发布事件

注入 `IEventBus<T>` 并直呼 `PublishAsync`：

```csharp
public class OrderService(IEventBus<OrderCreatedIntegrationEvent> eventBus)
{
    public async Task CreateOrderAsync()
    {
        var orderEvent = new OrderCreatedIntegrationEvent(Guid.NewGuid(), 100m, "user-123");
        
        // 发布后，所有订阅了该事件的微服务都将收到消息
        await eventBus.PublishAsync(orderEvent);
    }
}
```

### 4. 消费事件

实现 `IEventHandler<T>` 接口，框架会自动处理 Redis 消息的反序列化与本地分发：

```csharp
public class PaymentHandler : IEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct)
    {
        Console.WriteLine($"收到订单创建事件，订单号: {@event.OrderId}, 关联ID: {@event.CorrelationId}");
        // 执行支付逻辑...
    }
}
```

---

## Saga 案例：正向流与补偿流

### 场景：下订单 -> 扣减库存 (跨服务)

#### 1. 启动服务 (Order Service)
```csharp
// 发布下单事件
var orderEvent = new OrderCreatedEvent(orderId, qty); // 继承 IntegrationEvent
await eventBus.PublishAsync(orderEvent);
```

#### 2. 处理服务 (Inventory Service)
```csharp
public class InventoryHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        try 
        {
            // 扣减库存逻辑...
            if(insufficient) throw new Exception("库存不足");
            
            // 成功：发布成功事件延续流程
            var successEvent = @event.CreateNextEvent(c => new InventoryReservedEvent(c));
            await _eventBus.PublishAsync(successEvent);
        }
        catch (Exception)
        {
            // 失败：发布补偿事件触发上游回滚
            var failEvent = @event.CreateNextEvent(c => new InventoryReserveFailedCompensateEvent(c));
            await _eventBus.PublishAsync(failEvent);
        }
    }
}
```

#### 3. 回滚处理 (Order Service)
```csharp
public class OrderRollbackHandler : IEventHandler<InventoryReserveFailedCompensateEvent>
{
    public async Task HandleAsync(InventoryReserveFailedCompensateEvent @event, CancellationToken ct)
    {
        // 根据 CorrelationId 找到原始订单并撤销
        await _orderRepo.CancelOrderAsync(@event.CorrelationId);
    }
}
```

## 注意事项

- **幂等性**：分布式环境下请确保 Handle 逻辑具备幂等性。
- **事件类型**：确保跨服务的集成事件类具有相同的 `FullName`（命名空间+类名），或者使用 `[EventScheme("YourName")]` 特性进行显式指定。
