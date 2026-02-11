# BuildingBlocks.MassTransit

该项目是基于 MassTransit 的分布式事件总线封装，旨在简化 RabbitMQ 的集成与配置，并支持与本地事件（MediatR）的混合使用。此外，它还提供了基础的事件溯源（Event Sourcing）支持和 Saga 状态机持久化（基于 MongoDB）。

## 功能特性

*   **开箱即用**：集成了 RabbitMQ 的连接、认证和重试机制。
*   **灵活配置**：通过 `appsettings.json` 进行参数化配置。
*   **统一发布**：提供 `IEventPublisher` 接口，统一处理集成事件（MQ）和本地事件（MediatR）。
*   **本地异步处理**：本地事件（`PublishLocalAsync`）采用基于 `Channel` 的后台任务模式，不占用主线程，实现真正的异步非阻塞处理。
*   **自动注册**：支持自动扫描指定程序集中的 Consumer、Saga 和 Saga State Machine。
*   **事件溯源**：提供 `IEventStore` 和 `EventSourcedAggregate` 支持基于 Redis Stream 的事件存储。
*   **Saga 持久化**：内置 MongoDB Saga Repository 支持，轻松实现分布式事务。
*   **事件回放**：提供 `IEventReplayer` 服务，支持事件回放和补偿。

---

## 快速开始：服务配置指南

### 1. 引用项目

确保你的微服务项目引用了 `BuildingBlocks.MassTransit`。

### 2. 标准配置 (appsettings.json)

在你的微服务（例如 `OrderService`）的 `appsettings.json` 中添加配置。

**基础配置（仅消息总线）：**

```json
{
  "MassTransit": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "ServiceName": "OrderService", // 建议每个服务设置唯一的 ServiceName
    "RetryCount": 3,
    "RetryIntervalSeconds": 5
  }
}
```

**完整配置（含 Saga 和事件溯源）：**

```json
{
  "MassTransit": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "ServiceName": "OrderService",
    
    // 性能与熔断配置
    "PrefetchCount": 16,
    "UseCircuitBreaker": true,
    "CircuitBreakerTripThreshold": 15,
    "CircuitBreakerResetIntervalSeconds": 60,

    // Redis 连接（用于事件溯源存储）
    "RedisConnectionString": "localhost:6379",

    // 分布式事务 Saga 配置 (MongoDB)
    "Saga": {
      "Enabled": true,
      "RepositoryType": "MongoDb",
      "MongoDb": {
        "ConnectionString": "mongodb://localhost:27017",
        "DatabaseName": "lexicraft_sagas",
        "CollectionName": "order_sagas" // 可选，指定集合名称
      }
    },

    // 事件溯源配置 (Redis Stream)
    "EventSourcing": {
      "Enabled": true,
      "StreamPrefix": "events:" 
    }
  }
}
```

### 3. 代码注册 (Program.cs)

在服务启动逻辑中注册 MassTransit：

```csharp
using BuildingBlocks.MassTransit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册 MassTransit
// 传入当前程序集 (typeof(Program).Assembly) 以自动扫描 Consumer 和 Saga
builder.Services.AddCustomMassTransit(
    builder.Configuration, 
    assemblies: new[] { typeof(Program).Assembly } 
);

var app = builder.Build();
```

---

## 集成事件 (IntegrationEvent) 使用指南

`IntegrationEvent` 是跨服务通信的基础单元。本库提供了一个基类 `IntegrationEvent`，它实现了 `MediatR.INotification` 接口，使其既可以作为分布式消息发送，也可以作为本地事件发布。

### 1. 定义事件

建议在 `Shared` 类库中定义集成事件，以便生产者和消费者都能引用。

```csharp
using BuildingBlocks.MassTransit.Abstractions;

public record OrderCreatedEvent(Guid OrderId, decimal Amount) : IntegrationEvent;
```

`IntegrationEvent` 基类会自动生成 `Id` (Guid) 和 `CreationDate` (DateTime)。

### 2. 发布事件

使用 `IEventPublisher` 接口来发布事件。

*   **发布到消息队列 (RabbitMQ)**：使用 `PublishAsync`
*   **发布到本地 (MediatR)**：使用 `PublishLocalAsync`（后台异步处理，非阻塞）

```csharp
public class OrderService(IEventPublisher publisher)
{
    public async Task CreateOrder(Guid orderId)
    {
        var evt = new OrderCreatedEvent(orderId, 100.00m);
        
        // 发送到 RabbitMQ，其他微服务可以订阅
        await publisher.PublishAsync(evt);
        
        // 发送到本地 MediatR，当前服务内的 Handler 可以处理
        await publisher.PublishLocalAsync(evt);
    }
}
```

### 3. 消费事件

#### 方式 A: 分布式消费者 (MassTransit)

用于跨服务处理。

```csharp
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderId = context.Message.OrderId;
        // ...
    }
}
```

#### 方式 B: 本地处理器 (MediatR)

用于当前服务内部的逻辑解耦。

```csharp
public class OrderCreatedLocalHandler : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // ...
    }
}

---

## 使用案例与预期结果 (Use Cases & Outcomes)

### 案例 1：跨服务消息通讯 (Pub/Sub)

**场景**：订单服务创建一个订单后，发布 `OrderCreatedEvent`，库存服务订阅该消息并扣减库存。

**代码实现**：

*   **定义事件** (Shared Library):
    ```csharp
    public record OrderCreatedEvent(Guid OrderId, DateTime CreatedAt);
    ```

*   **发布者 (OrderService)**:
    ```csharp
    public class OrderService(IEventPublisher publisher)
    {
        public async Task CreateOrder(Guid orderId)
        {
            // 业务逻辑...
            // 发布集成事件
            await publisher.PublishAsync(new OrderCreatedEvent(orderId, DateTime.UtcNow));
        }
    }
    ```

*   **消费者 (InventoryService)**:
    ```csharp
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var orderId = context.Message.OrderId;
            Console.WriteLine($"收到订单创建事件: {orderId}，正在扣减库存...");
        }
    }
    ```

**预期结果**：
1.  **RabbitMQ**：在 RabbitMQ 管理界面可以看到一个名为 `OrderCreatedEvent` 的 Exchange，以及绑定到 InventoryService 的 Queue。
2.  **日志**：OrderService 输出 "Published OrderCreatedEvent"，InventoryService 输出 "收到订单创建事件..."。
3.  **行为**：消息实现了异步解耦，OrderService 无需等待 InventoryService 完成即可返回。

---

### 案例 2：分布式事务 (Saga 模式)

**场景**：处理长流程订单。订单创建 -> 扣款 -> 发货。如果扣款失败，需要执行补偿（取消订单）。

**代码实现**：

*   **定义状态机 (OrderStateMachine)**:
    ```csharp
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));

            Initially(
                When(OrderSubmitted)
                    .Then(ctx => ctx.Saga.OrderDate = DateTime.UtcNow)
                    .TransitionTo(Submitted)
                    .Publish(ctx => new ProcessPaymentEvent(ctx.Saga.CorrelationId)) // 触发下一步
            );
            
            // ... 后续状态流转与补偿逻辑
        }
        // ... 状态与事件定义
    }
    ```

**预期结果**：
1.  **MongoDB**：在配置的数据库（如 `lexicraft_sagas`）中，`order_sagas` 集合会新增一条记录。
    *   字段包括 `CorrelationId` (订单ID), `CurrentState` ("Submitted"), `Version` 等。
2.  **流程追踪**：随着流程推进（如支付成功），MongoDB 中的 `CurrentState` 字段会自动更新为 "Paid" 或 "Shipped"。
3.  **容错**：如果中间步骤抛出异常且配置了补偿逻辑，Saga 会自动流转到 "Faulted" 或执行回滚操作，保证数据最终一致性。

---

### 案例 3：事件溯源 (Event Sourcing)

**场景**：需要记录对象的所有状态变更历史，而不仅仅是当前状态。例如审计日志或复杂聚合根。

**代码实现**：

*   **聚合根**:
    ```csharp
    public class UserProfile : EventSourcedAggregate
    {
        public void UpdateEmail(string newEmail)
        {
            AddEvent(new EmailUpdatedEvent(Id, newEmail));
        }
    }
    ```

*   **保存事件**:
    ```csharp
    await _eventStore.AppendEventsAsync($"user-{userId}", userProfile.GetUncommittedEvents());
    ```

**预期结果**：
1.  **Redis**：连接到配置的 Redis，使用命令 `XRANGE events:user-{userId} - +` 查看。
2.  **数据结构**：会看到一个 Stream（流），其中包含多条 Entry，每条 Entry 代表一个事件（EmailUpdatedEvent），包含时间戳、事件类型和 JSON 数据。
3.  **重构状态**：你可以随时通过读取这些事件流来还原 `UserProfile` 在任意时间点的状态。

---

### 案例 4：事件回放 (Event Replay)

**场景**：系统升级后需要重新处理过去一周的数据，或者修复了一个 Bug 需要重新计算报表。

**代码实现**：

```csharp
// 回放特定聚合根的所有历史事件
await _eventReplayer.ReplayAsync(
    streamId: "user-123",
    publishMethod: async (evt, meta) => 
    {
        // 这里可以将事件重新投递到消息队列，或者调用新的处理逻辑
        await _publisher.PublishAsync(evt);
    }
);
```

**预期结果**：
1.  **触发处理**：系统会从 Redis 读取该 Stream 的所有历史事件。
2.  **重新执行**：消费者会再次收到这些事件（仿佛它们刚刚发生一样），从而触发新的业务逻辑或数据更新。
3.  **元数据**：在回放的事件中，通常会包含 `IsReplay = true` 的元数据，消费者可以据此判断是否需要跳过某些副作用（如发送邮件）。

---

## 常见问题排查

1.  **连接失败**：检查 `appsettings.json` 中的 Host, Port, Username, Password 是否正确，确保 RabbitMQ/Redis/MongoDB 服务已启动。
2.  **Consumer 未收到消息**：
    *   检查 Queue 是否在 RabbitMQ 中创建。
    *   确保 `Program.cs` 中注册时包含了 Consumer 所在的程序集。
    *   检查 Exchange 和 Queue 的绑定关系。
3.  **Saga 未持久化**：
    *   确保 `Saga:Enabled` 为 `true`。
    *   确保 MongoDB 连接字符串正确且数据库可写。
