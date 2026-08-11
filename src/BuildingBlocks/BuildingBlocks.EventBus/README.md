# BuildingBlocks.EventBus

`BuildingBlocks.EventBus` 是 LexiCraft 的轻量事件分发组件，提供：

- 基于有界 `Channel` 的进程内事件队列；
- 基于 Redis Pub/Sub 的跨进程事件通知；
- 按事件类型在本地与 Redis 之间自动选择的 `IEventBus<TEvent>`；
- `CorrelationId` 传递、处理器作用域、JSON 序列化和 Redis 幂等键。

> Redis Pub/Sub 不持久化消息，也不提供确认、重试或离线补偿。需要可靠跨服务投递时，应使用 `BuildingBlocks.MassTransit` 与 RabbitMQ。

## 1. 分发规则

| 调用 | 行为 |
| --- | --- |
| `PublishAsync` | 当事件实现 `ISagaIntegrationEvent` 且启用 Redis 时发往 Redis；否则在启用本地队列时发往本地队列。 |
| `PublishLocalAsync` | 强制进入本地队列；本地队列关闭时记录警告并返回。 |
| `PublishDistributedAsync` | 强制发布到 Redis；Redis 发布失败会抛出 `EventClientException`。 |

`IntegrationEvent` 已实现 `ISagaIntegrationEvent`，因此继承该基类的事件在 Redis 启用时会走分布式路径。

如果本地与 Redis 都未启用，`PublishAsync` 只记录警告，不会发送事件。

## 2. 资源与生命周期保证

本次实现采用以下约束，防止异常任务或无界队列持续占用线程和内存：

- 本地事件统一进入一个有界 `Channel`，默认容量为 `1024`；
- 本地队列满时发布方通过 `WriteAsync` 异步等待，不创建轮询任务，不无限扩容；
- 本地只有一个固定消费循环，同一进程内按入队顺序处理事件；
- 每条事件创建独立异步 DI 作用域，处理完成后立即释放作用域内资源；
- Redis 回调不再为每条消息创建 `Task.Run`，而是写入有界缓冲区，由固定数量工作器消费；
- Host 停止时会完成本地写入端、取消消费循环、取消 Redis 订阅，并释放组件专用 Redis 连接；
- 所有发布和处理 API 都传递 `CancellationToken`。

### 2.1 过载行为

- **本地队列**：容量满时等待，由调用方传入的 `CancellationToken` 控制最长等待时间；若本地处理器在队列已满时再次发布，则立即抛出 `EventClientException`，避免唯一消费者等待自己释放容量。
- **Redis 消费缓冲区**：Redis 的订阅回调不能安全地无限等待；缓冲区满时会丢弃新消息，并在第 1 条及之后每累计 100 条时记录警告。
- **Redis Pub/Sub**：消息是至多一次投递。进程未订阅、连接中断或本地缓冲区过载时，消息不会补发。

关键业务事件不要依赖 Redis Pub/Sub 的丢弃策略，应迁移到 MassTransit/RabbitMQ。

## 3. 配置

配置节名称固定为 `EventBusOptions`：

```json
{
  "EventBusOptions": {
    "EnableLocal": true,
    "EnableRedis": false,
    "Local": {
      "Capacity": 1024
    },
    "Redis": {
      "ConnectionString": "${EVENT_BUS_REDIS_CONNECTION}",
      "Prefix": "lexi",
      "IdempotencyExpireSeconds": 86400,
      "ConsumerQueueCapacity": 1024,
      "ConsumerConcurrency": 1
    }
  }
}
```

不要把真实 Redis 密码提交到配置文件。连接字符串应由环境变量、用户机密或部署平台密钥注入。

### 3.1 配置项

| 配置项 | 默认值 | 说明 |
| --- | ---: | --- |
| `EnableLocal` | `true` | 是否注册本地队列和后台消费者。 |
| `EnableRedis` | `false` | 是否创建 Redis 专用连接并订阅集成事件频道。 |
| `Local.Capacity` | `1024` | 本地有界队列容量，必须大于 `0`。 |
| `Redis.ConnectionString` | `null` | Redis 连接字符串；启用 Redis 时必填。 |
| `Redis.Prefix` | `lexi` | Redis 频道与幂等键前缀。 |
| `Redis.IdempotencyExpireSeconds` | `86400` | 集成事件幂等键有效期，必须大于 `0`。 |
| `Redis.ConsumerQueueCapacity` | `1024` | Redis 消费缓冲区容量，必须大于 `0`。 |
| `Redis.ConsumerConcurrency` | `1` | Redis 固定消费工作器数量，必须大于 `0`。大于 `1` 时不保证跨消息处理顺序。 |

配置在 `AddZEventBus` 执行时确定。修改队列容量、开关或连接字符串后应重启服务，不依赖运行时热切换。

## 4. 注册

在宿主项目的 `Program.cs` 中注册：

```csharp
using BuildingBlocks.EventBus.Extensions;

builder.AddZEventBus();
```

也可以在代码中追加配置：

```csharp
builder.AddZEventBus(options =>
{
    options.EnableLocal = true;
    options.Local.Capacity = 512;
});
```

事件处理器不会自动加入 DI。业务项目必须按自身生命周期显式注册：

```csharp
builder.Services.AddScoped<IEventHandler<UserRegisteredIntegrationEvent>,
    UserRegisteredIntegrationEventHandler>();
```

## 5. 定义与处理事件

```csharp
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Shared;

[EventScheme("identity.user-registered")]
public sealed record UserRegisteredIntegrationEvent(Guid UserId)
    : IntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

public sealed class UserRegisteredIntegrationEventHandler
    : IEventHandler<UserRegisteredIntegrationEvent>
{
    public Task HandleAsync(
        UserRegisteredIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        // 执行业务逻辑；异步 I/O 必须继续传递 cancellationToken。
        return Task.CompletedTask;
    }
}
```

发布事件：

```csharp
public sealed class UserRegistrationNotifier(
    IEventBus<UserRegisteredIntegrationEvent> eventBus)
{
    public ValueTask NotifyAsync(Guid userId, CancellationToken cancellationToken)
    {
        return eventBus.PublishAsync(
            new UserRegisteredIntegrationEvent(userId),
            cancellationToken);
    }
}
```

## 6. EventSchemeAttribute

`EventSchemeAttribute.EventName` 用于覆盖 Redis 频道名：

```csharp
[EventScheme("identity.user-registered")]
public sealed record UserRegisteredIntegrationEvent(Guid UserId)
    : IntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
```

Redis 频道最终为：

```text
{Redis.Prefix}:{EventName}
```

例如 `lexi:identity.user-registered`。

`SingleReader`、`SingleWriter`、`AllowSynchronousContinuations` 属性仅为旧调用代码保留。当前本地总线使用全局有界队列，并固定为单消费者、多发布者、禁止同步 continuation，这些属性不再改变 Channel 行为。

## 7. Redis 幂等与异常语义

只有反序列化后属于 `IntegrationEvent` 的消息会写入幂等键：

```text
{Prefix}:idempotency:{EventType}:{EventId}
```

使用 Redis `SET NX` 与过期时间防止同一事件 ID 在有效期内被重复处理。需要注意：

- 幂等键在调用处理器前写入；
- 某个处理器失败时会记录错误并继续执行其他处理器；
- Redis Pub/Sub 不会因为处理器失败而重投；
- 本地处理器异常会被记录，不会终止整个消费循环；
- Redis 发布或序列化失败会向调用方抛出异常，调用方不能把失败当作成功。

如果业务要求“处理失败可重试、最终进入错误队列”，应使用 MassTransit 的重试与错误队列能力。

## 8. Saga CorrelationId 传递

```csharp
var nextEvent = currentEvent.CreateNextEvent(correlationId =>
    new PaymentRequestedIntegrationEvent(
        Guid.NewGuid(),
        correlationId,
        DateTime.UtcNow,
        orderId));

await eventBus.PublishAsync(nextEvent, cancellationToken);
```

`CreateNextEvent` 只负责传递 `CorrelationId`，不提供状态持久化、补偿或可靠消息事务。

## 9. 验证

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.EventBus\BuildingBlocks.EventBus.csproj --no-restore
dotnet test src\BuildingBlocks\BuildingBlocks.Messaging.Tests\BuildingBlocks.Messaging.Tests.csproj --no-restore
git diff --check
```

回归测试覆盖本地队列背压、单消费者处理以及停止时释放等待中的发布方。
