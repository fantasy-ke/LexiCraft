# BuildingBlocks.MassTransit

`BuildingBlocks.MassTransit` 封装本平台的可靠消息能力，包含：

- MassTransit 8 + RabbitMQ 的发布、Consumer 自动注册、重试和断路器；
- 基于有界 `Channel` 与 MediatR 的进程内领域事件队列；
- 可选的 MongoDB Saga Repository；
- 可选的 Redis Stream 事件存储与流式回放。

该组件采用**显式启用**策略：未配置 `MassTransit` 节，或 `MassTransit:Enabled=false` 时，不注册 RabbitMQ Bus、`IEventPublisher`、本地事件后台服务、Saga 存储或事件存储。

## 1. 组件边界

| 能力 | 实现 | 投递语义 |
| --- | --- | --- |
| 跨服务集成事件 | MassTransit + RabbitMQ | 持久队列、确认、重试与错误队列由 MassTransit/RabbitMQ 管理。 |
| 进程内领域事件 | 有界 `Channel` + MediatR | 仅存在于当前进程；进程崩溃会丢失未处理事件。 |
| Saga 状态 | MassTransit MongoDB Repository | 仅在 `Saga.Enabled=true` 时初始化。 |
| 事件溯源 | Redis Stream | 仅在 `EventSourcing.Enabled=true` 时创建独立 Redis 连接。 |

`BuildingBlocks.EventBus` 的 Redis Pub/Sub 更轻量但不可靠；需要跨服务可靠投递时优先使用本类库。

## 2. 资源与生命周期保证

为避免异常任务、连接误用和无界内存增长，当前实现遵循以下规则：

- `MassTransit.Enabled`、`Saga.Enabled`、`EventSourcing.Enabled` 默认均为 `false`；
- 本地领域事件使用有界队列，默认容量 `1024`，队列满时发布方异步等待；
- 本地队列只有一个固定后台消费循环，不为每条事件创建后台任务；
- Host 停止时先关闭本地写入端，释放等待队列容量的发布方，再取消后台消费者；
- 每条本地事件在独立异步 DI 作用域中处理，作用域结束后释放数据库上下文等资源；
- RabbitMQ 使用 `PrefetchCount` 与可选 `ConcurrencyLimit` 限制单实例在途消息数量；
- 事件存储使用专用 `EventStoreRedisConnection`，不会误复用缓存或其他组件注册的 `IConnectionMultiplexer`；
- Redis Stream 按 `ReadBatchSize` 分页读取，回放不会一次性把完整事件流加载到内存；
- 发布、存储、读取和回放链路均传递 `CancellationToken`。

## 3. 配置

### 3.1 只启用 RabbitMQ

```json
{
  "MassTransit": {
    "Enabled": true,
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "${RABBITMQ_USERNAME}",
    "Password": "${RABBITMQ_PASSWORD}",
    "PrefetchCount": 16,
    "ConcurrencyLimit": 8,
    "RetryCount": 3,
    "RetryIntervalSeconds": 5,
    "UseCircuitBreaker": false,
    "LocalEvents": {
      "Capacity": 1024
    },
    "Saga": {
      "Enabled": false
    },
    "EventSourcing": {
      "Enabled": false
    }
  }
}
```

### 3.2 启用可选 Saga 与事件溯源

```json
{
  "MassTransit": {
    "Enabled": true,
    "Host": "rabbitmq",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "${RABBITMQ_USERNAME}",
    "Password": "${RABBITMQ_PASSWORD}",
    "PrefetchCount": 16,
    "ConcurrencyLimit": 8,
    "RetryCount": 3,
    "RetryIntervalSeconds": 5,
    "UseCircuitBreaker": true,
    "CircuitBreakerTripThreshold": 15,
    "CircuitBreakerActiveThreshold": 10,
    "CircuitBreakerResetIntervalSeconds": 60,
    "LocalEvents": {
      "Capacity": 1024
    },
    "Saga": {
      "Enabled": true,
      "RepositoryType": "MongoDb",
      "MongoDb": {
        "ConnectionString": "${SAGA_MONGODB_CONNECTION}",
        "DatabaseName": "fantasy_sagas",
        "CollectionName": "service_sagas"
      }
    },
    "EventSourcing": {
      "Enabled": true,
      "RedisConnectionString": "${EVENT_STORE_REDIS_CONNECTION}",
      "StreamPrefix": "events:",
      "ReadBatchSize": 256
    }
  }
}
```

不要向仓库提交真实用户名、密码或完整连接字符串。使用环境变量、用户机密或部署平台密钥注入。

### 3.3 配置项

| 配置项 | 默认值 | 约束/说明 |
| --- | ---: | --- |
| `Enabled` | `false` | 必须显式设为 `true` 才注册组件。 |
| `Host` | `localhost` | 启用时不能为空。 |
| `Port` | `5672` | 必须在 `1-65535` 范围内。 |
| `VirtualHost` | `/` | 启用时不能为空。 |
| `PrefetchCount` | `16` | 必须大于 `0`；限制 RabbitMQ 预取消息数。 |
| `ConcurrencyLimit` | `null` | 可选；配置时必须大于 `0`。 |
| `RetryCount` | `3` | 必须大于等于 `0`；`0` 表示不安装重试策略。 |
| `RetryIntervalSeconds` | `5` | 必须大于等于 `0`。 |
| `UseCircuitBreaker` | `false` | 是否安装 MassTransit 断路器。 |
| `CircuitBreakerTripThreshold` | `15` | 启用断路器时必须在 `1-100` 范围内。 |
| `CircuitBreakerActiveThreshold` | `10` | 启用断路器时必须大于 `0`。 |
| `CircuitBreakerResetIntervalSeconds` | `60` | 启用断路器时必须大于 `0`。 |
| `LocalEvents.Capacity` | `1024` | 本地有界队列容量，必须大于 `0`。 |
| `Saga.Enabled` | `false` | 显式启用 MongoDB Saga Repository。 |
| `EventSourcing.Enabled` | `false` | 显式启用 Redis Stream 事件存储。 |
| `EventSourcing.StreamPrefix` | `events:` | Redis Stream Key 前缀，启用时不能为空。 |
| `EventSourcing.ReadBatchSize` | `256` | 流式读取批大小，必须大于 `0`。 |

`ServiceName` 当前仅作为业务侧描述性配置保留，不会自动修改 endpoint 名称。端点命名仍使用 MassTransit 的 kebab-case formatter；需要额外命名规则时通过注册回调配置。

配置在服务启动注册阶段确定。修改开关、连接、队列容量或并发参数后应重启服务。

## 4. 注册

```csharp
using BuildingBlocks.MassTransit.Extensions;

builder.Services.AddCustomMassTransit(
    builder.Configuration,
    [typeof(Program).Assembly]);
```

如果 Consumer 或 Saga 位于其他程序集，将对应程序集一起传入：

```csharp
builder.Services.AddCustomMassTransit(
    builder.Configuration,
    [
        typeof(Program).Assembly,
        typeof(OrderSubmittedConsumer).Assembly
    ],
    registration =>
    {
        // 可在此追加 Consumer、Saga 或 endpoint 级注册。
    });
```

当 `Enabled=false` 时，上述调用是安全的空操作；业务代码也不能解析 `IEventPublisher`。

## 5. 发布与消费跨服务事件

### 5.1 定义事件

```csharp
using BuildingBlocks.MassTransit.Abstractions;

public sealed record UserRegisteredIntegrationEvent(Guid UserId)
    : IntegrationEvent;
```

### 5.2 发布

```csharp
using BuildingBlocks.MassTransit.Services;

public sealed class UserRegistrationNotifier(IEventPublisher eventPublisher)
{
    public Task NotifyAsync(Guid userId, CancellationToken cancellationToken)
    {
        return eventPublisher.PublishAsync(
            new UserRegisteredIntegrationEvent(userId),
            cancellationToken);
    }
}
```

发布任务会向调用方传播失败和取消，调用方不能忽略返回的 `Task`。

### 5.3 Consumer

```csharp
using MassTransit;

public sealed class UserRegisteredConsumer
    : IConsumer<UserRegisteredIntegrationEvent>
{
    public Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        return HandleAsync(context.Message, context.CancellationToken);
    }

    private static Task HandleAsync(
        UserRegisteredIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

指定程序集中的 Consumer 会通过 `AddConsumers` 扫描并使用 kebab-case endpoint 名称注册。

## 6. 本地领域事件

`IEventPublisher.PublishLocalAsync` 将 `IDomainEvent` 写入有界队列，由 `LocalEventBackgroundService` 顺序交给 MediatR：

```csharp
await eventPublisher.PublishLocalAsync(domainEvent, cancellationToken);
```

行为说明：

- 队列满时调用会等待，调用方应传入具有合理超时或请求生命周期的 `CancellationToken`；若本地处理器在队列已满时再次发布，则立即抛出 `InvalidOperationException`，避免唯一消费者等待自己释放容量；
- 单消费者保证当前进程内的入队顺序，不保证多个服务实例之间的顺序；
- 单个 MediatR 处理器失败时会记录错误，后台循环继续处理后续事件；
- Host 正常停止会关闭写入端，正在等待容量的发布方收到 `ChannelClosedException`；
- 本地队列不持久化，不能代替 RabbitMQ 或数据库 Outbox。

## 7. Saga

启用 Saga 时，仅支持 `SagaRepositoryType.MongoDb`。不支持的仓储类型会在启动注册阶段直接抛出 `NotSupportedException`，不会静默跳过持久化。

业务项目仍需：

1. 定义 MassTransit Saga State Machine 与 Saga State；
2. 将其程序集传入 `AddCustomMassTransit`；
3. 显式配置 `Saga.Enabled=true`；
4. 为 MongoDB 使用可写且不包含硬编码密钥的连接配置。

Saga 的幂等、关联、补偿和最终状态属于业务状态机职责，本类库只负责 Repository 接入。

## 8. Redis Stream 事件存储

启用 `EventSourcing` 后注册：

- `IEventStore` / `RedisEventStore`；
- `IEventReplayer`：把历史事件重新发布到 MassTransit；
- `IDomainEventReplayer`：把历史领域事件重新发布到 MediatR；
- 事件存储专用 Redis 连接。

### 8.1 自动记录本地事件

同时实现 `IDomainEvent` 与 `IEventSourced` 的本地事件会在 MediatR 分发前写入 Redis Stream：

```csharp
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;

public sealed record EmailChangedDomainEvent(Guid UserId, string Email)
    : IDomainEvent, IEventSourced
{
    public string GetStreamId() => $"user-{UserId}";

    public IDictionary<string, object>? GetMetaData()
    {
        return new Dictionary<string, object>
        {
            ["source"] = "identity"
        };
    }
}
```

事件存储失败会记录错误，但当前本地事件仍会继续交给 MediatR。若业务要求“存储失败则不得执行业务处理”，应在应用层显式调用 `IEventStore.AppendEventsAsync` 并决定事务边界，而不是依赖自动记录。

### 8.2 显式追加与流式读取

```csharp
await eventStore.AppendEventsAsync(
    streamId,
    events,
    expectedVersion,
    cancellationToken);

await foreach (var storedEvent in eventStore.StreamStoredEventsAsync(
                   streamId,
                   fromVersion: 0,
                   toVersion: null,
                   cancellationToken))
{
    // 逐条处理，不把完整 Stream 物化到内存。
}
```

`ReadStoredEventsAsync` 与 `ReadEventsAsync` 为兼容 API，会把结果物化为集合；长事件流优先使用 `StreamStoredEventsAsync`。

### 8.3 回放

```csharp
await eventReplayer.ReplayAsync(
    streamId,
    fromVersion: 0,
    toVersion: null,
    cancellationToken);
```

`IEventReplayer` 发布的消息会带有以下 Header：

- `MT-Event-Replay=true`；
- `MT-Original-MessageId`；
- `MT-Original-Timestamp`；
- `MT-Stream-Version`；
- 可选 `MT-Original-MetaData`。

消费者应根据这些 Header 避免重复发送邮件、扣款等不可重复副作用。

## 9. 一致性与已知限制

- `expectedVersion` 会在追加前读取当前 Stream 长度并检查，但“检查 + 多条追加”不是跨进程原子事务；同一 Stream 存在多个并发写入者时，仍需 Lua、分布式锁或单写者约束。
- 一次 `AppendEventsAsync` 中的多条事件逐条写入；中途失败可能形成部分追加，调用方必须设计幂等事件 ID 与恢复策略。
- Redis Stream 不会由本类库自动裁剪。事件历史用于聚合重建时，不应随意设置会删除历史的保留策略。
- RabbitMQ 发布与业务数据库提交之间没有自动事务一致性。需要原子提交时，应在业务服务中接入 MassTransit Transactional Outbox 或等价 Outbox 方案。
- 本地事件只保证进程内顺序和内存背压，不提供持久化、重试或错误队列。

## 10. 常见问题

### 未连接 RabbitMQ

确认 `MassTransit.Enabled=true`，并检查 Host、Port、VirtualHost、用户名、密码和网络可达性。没有 `MassTransit` 配置节时组件不会尝试连接默认的 `guest@localhost:5672`。

### Consumer 未创建队列

确认 Consumer 所在程序集已传入 `AddCustomMassTransit`，并检查实际 kebab-case endpoint 名称及 RabbitMQ 权限。

### Saga 未持久化

确认 `Saga.Enabled=true`、RepositoryType 为 `MongoDb`，并验证数据库与集合配置。只启用 MassTransit 不会自动初始化 Saga。

### 事件存储未写入

确认 `EventSourcing.Enabled=true`，事件实现 `IEventSourced`，且事件通过本地事件队列处理；直接发布 RabbitMQ 集成事件不会自动写入 Redis Stream。

### 本地发布长期等待

说明本地消费者处理速度低于发布速度且队列已满。检查慢处理器、外部 I/O 超时和 `LocalEvents.Capacity`。不要仅通过无限增大容量掩盖吞吐问题。

## 11. 验证

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.MassTransit\BuildingBlocks.MassTransit.csproj --no-restore
dotnet test src\BuildingBlocks\BuildingBlocks.Messaging.Tests\BuildingBlocks.Messaging.Tests.csproj --no-restore
git diff --check
```

回归测试覆盖有界队列背压、停止时释放等待中的发布方，以及可选后端默认关闭。
