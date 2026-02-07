# BuildingBlocks.MassTransit

该项目是基于 MassTransit 的分布式事件总线封装，旨在简化 RabbitMQ 的集成与配置，并支持与本地事件（MediatR）的混合使用。

## 功能特性

*   **开箱即用**：集成了 RabbitMQ 的连接、认证和重试机制。
*   **灵活配置**：通过 `appsettings.json` 进行参数化配置。
*   **统一发布**：提供 `IEventPublisher` 接口，统一处理集成事件（MQ）和本地事件（MediatR）。
*   **自动注册**：支持自动扫描指定程序集中的 Consumer。

## 安装与配置

### 1. 引用项目

确保你的服务引用了 `BuildingBlocks.MassTransit` 项目。

### 2. 添加配置

在 `appsettings.json` 中添加 MassTransit 配置节点：

```json
{
  "MassTransit": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "ServiceName": "MyService", // 可选，用于队列命名前缀
    "RetryCount": 3,
    "RetryIntervalSeconds": 5
  }
}
```

### 3. 注册服务

在 `Program.cs` 或 `Startup.cs` 中注册服务：

```csharp
using BuildingBlocks.MassTransit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册 MassTransit
builder.Services.AddCustomMassTransit(
    builder.Configuration, 
    assemblies: new[] { typeof(Program).Assembly }, // 指定包含 Consumer 的程序集
    configure: bus => 
    {
        // 可选：添加 Saga 或其他高级配置
        // bus.AddSagaStateMachine<OrderStateMachine, OrderState>();
    });
```

## 使用指南

### 1. 定义事件

继承 `IntegrationEvent` 基类。由于 `IntegrationEvent` 实现了 `INotification`，它既可以作为集成事件发送，也可以作为本地事件发布。

```csharp
using BuildingBlocks.MassTransit.Abstractions;

public record OrderCreatedIntegrationEvent(Guid OrderId, decimal Amount) : IntegrationEvent;
```

### 2. 定义消费者

实现 `IConsumer<T>` 接口处理消息。

```csharp
using MassTransit;

public class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        _logger.LogInformation("Received Order Created Event: {OrderId}", context.Message.OrderId);
        
        // 业务逻辑处理...
    }
}
```

### 3. 发布事件

推荐使用 `IEventPublisher` 统一接口。

```csharp
using BuildingBlocks.MassTransit.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;

    public OrdersController(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedIntegrationEvent(orderId, 100.00m);

        // 场景 A: 发布集成事件 (发送到 RabbitMQ)
        await _eventPublisher.PublishAsync(evt);

        // 场景 B: 发布本地事件 (进程内 MediatR)
        // 适用于不需要跨服务，仅在当前服务内不同模块间解耦的场景
        await _eventPublisher.PublishLocalAsync(evt);

        return Ok(new { OrderId = orderId });
    }
}
```

### 4. 混合使用：在 Consumer 中发布本地事件

在处理集成事件后，如果需要触发本地的其他业务逻辑（例如触发多个独立的 Handler），可以发布本地事件。

```csharp
public class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly IEventPublisher _eventPublisher;

    public OrderCreatedConsumer(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        // 1. 处理核心逻辑
        Console.WriteLine($"Processing external order: {context.Message.OrderId}");

        // 2. 触发本地事件，通知其他模块 (例如发送邮件、更新缓存等)
        // 注意：这里使用的是 PublishLocalAsync
        await _eventPublisher.PublishLocalAsync(context.Message);
    }
}
```

## 核心接口说明

*   **`IIntegrationEvent`**: 所有集成事件的基类，包含 `Id` 和 `CreationDate`，并继承自 `INotification`。
*   **`IEventPublisher`**: 
    *   `PublishAsync<T>(T @event)`: 发送到消息队列 (MassTransit `IPublishEndpoint`)。
    *   `PublishLocalAsync<T>(T @event)`: 发送到本地进程 (MediatR `IPublisher`)。
