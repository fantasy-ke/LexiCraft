# BuildingBlocks.Idempotency 幂等处理组件

为 ASP.NET Core（含 .NET Aspire 服务）提供可复用的**请求幂等**基础能力：在带 `[Idempotent]` / `IdempotentAttribute` 元数据的端点上，依据客户端 `Idempotency-Key` 与请求指纹，防止重试或重复提交导致副作用被重复执行。

> 当前状态：本组件为**基础组件**，已包含完整实现与单元测试，但**尚未接入** Identity / Vocabulary / Practice / Files 等业务端点。业务服务接入时只需声明属性并启用中间件，详见下文“接入方式”。本文档不会声称任何业务服务已启用幂等。

---

## 1. 它能解决什么问题

- 客户端在网络超时后重试 `POST /payments`，导致重复扣款。
- 用户快速双击提交，产生两条重复订单。
- 网关/负载均衡的自动重试触发重复副作用。

幂等组件保证：**相同 `Idempotency-Key` 的重复请求，其业务逻辑只成功执行一次**；后续相同请求要么重放首次成功响应，要么直接返回冲突，由 `IdempotencyMode` 决定。

---

## 2. 核心概念

| 概念 | 说明 |
| --- | --- |
| **幂等键（Idempotency-Key）** | 客户端在请求头中携带的、代表“一次业务操作”的标识。相同键视为同一次操作。 |
| **请求指纹（Fingerprint）** | 由 请求方法 + PathBase + Path + QueryString + Content-Type + 请求体 计算的 SHA-256 摘要。用于检测“同一键但内容不同”的冲突。 |
| **存储键（Storage Key）** | 由 **用户作用域**（已认证用户标识或 `anonymous`）+ 方法 + 路径 + 客户端键 哈希得到。因此**幂等键按用户隔离**，不同用户的相同键互不冲突。 |
| **租约（Lease）** | 请求处理期间的临时所有权凭证，带随机 `OwnerToken`，用于并发安全与过期保护。 |
| **完成记录（Completed）** | 首次成功处理后保存的状态与可选响应体，供后续相同请求重放或拒绝。 |

默认存储实现为 `RedisIdempotencyStore`（基于 Redis + Lua 原子脚本），通过 `IRedisConnectionFactory` 获取数据库，并依赖哈希标签把租约键与结果键放在同一槽，保证完成/放弃操作原子。

---

## 3. 接入方式

### 3.1 依赖

- 必须先注册缓存组件（`AddCaching`），因为默认 Redis 实现依赖 `IRedisConnectionFactory`：

  ```csharp
  builder.AddCaching();      // 必须先于 AddIdempotency 注册
  builder.AddIdempotency();  // 注册 IdempotencyOptions 与 RedisIdempotencyStore
  ```

- 若希望使用自定义存储（例如内存、PostgreSQL），在调用 `AddIdempotency()` **之前**注册 `IIdempotencyStore` 即可，组件会跳过默认 Redis 注册：

  ```csharp
  builder.Services.AddSingleton<IIdempotencyStore, MyIdempotencyStore>();
  builder.AddIdempotency();
  ```

### 3.2 完整管线

`UseIdempotency()` 必须在**路由匹配之后、端点执行之前**调用：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCaching();
builder.AddIdempotency();

var app = builder.Build();

app.UseRouting();          // 先匹配路由，使端点元数据可用
app.UseIdempotency();      // 启用幂等中间件
app.MapGroup("/payments").WithOpenApi();
// ... 其余端点与中间件

app.Run();
```

### 3.3 注册校验

`AddIdempotency()` 会在启动时（`ValidateOnStart`）校验配置：

- `HeaderName` / `Prefix` 不能为空；
- `Retention`、`ProcessingTimeout`、`ReplayPollInterval` 至少为 1 毫秒；
- `ReplayWaitTimeout` 不能为负；
- `MaxRequestBodyBytes` 必须大于 0，`MaxResponseBodyBytes` 介于 1 与 `int.MaxValue` 之间，`MaxKeyLength` 大于 0。

---

## 4. 配置

在 `appsettings.json` 的 `Idempotency` 节（对应 `IdempotencyOptions.SectionName`）配置全局默认值。端点属性可逐项覆盖，值为 `0` 表示回退到全局配置。

```json
{
  "Idempotency": {
    "HeaderName": "Idempotency-Key",
    "Prefix": "lexicraft:idempotency",
    "RedisInstanceName": null,
    "Retention": "00:10:00",
    "ProcessingTimeout": "00:02:00",
    "ReplayWaitTimeout": "00:00:03",
    "ReplayPollInterval": "00:00:00.050",
    "MaxRequestBodyBytes": 1048576,
    "MaxResponseBodyBytes": 1048576,
    "MaxKeyLength": 200
  }
}
```

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `HeaderName` | `Idempotency-Key` | 客户端传递幂等键的请求头名称。 |
| `Prefix` | `lexicraft:idempotency` | Redis 键的公共前缀。 |
| `RedisInstanceName` | `null` | 命名 Redis 实例；空表示默认实例。 |
| `Retention` | 10 分钟 | 成功记录的默认保留时间。 |
| `ProcessingTimeout` | 2 分钟 | 执行租约的默认有效期。 |
| `ReplayWaitTimeout` | 3 秒 | Replay 模式等待并发首请求完成的最长时间；`0` 表示不等待。 |
| `ReplayPollInterval` | 50 毫秒 | Replay 模式轮询幂等状态的时间间隔。 |
| `MaxRequestBodyBytes` | 1 MB | 参与指纹计算的最大请求体字节数，超出返回 413。 |
| `MaxResponseBodyBytes` | 1 MB | 可在内存中捕获并重放的最大响应体字节数，超出则标记不可重放。 |
| `MaxKeyLength` | 200 | 去空白后的幂等键最大字符数，超出返回 400。 |

---

## 5. 声明幂等端点

### 5.1 Minimal API

```csharp
app.MapPost("/payments", async (PaymentRequest request, IPaymentService svc) =>
    {
        var id = await svc.ChargeAsync(request);
        return Results.Created($"/payments/{id}", new { id });
    })
    .WithMetadata(new IdempotentAttribute(IdempotencyMode.Replay))
    .WithName("CreatePayment");
```

带覆盖参数的声明：

```csharp
.WithMetadata(new IdempotentAttribute
{
    Mode = IdempotencyMode.Reject,
    RequireKey = true,
    RetentionSeconds = 600,
    ProcessingTimeoutSeconds = 120,
    ReplayWaitTimeoutMilliseconds = 2000
});
```

### 5.2 MVC / 控制器

```csharp
[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    [HttpPost]
    [Idempotent(IdempotencyMode.Replay)]
    public async Task<IActionResult> Create(PaymentRequest request) { /* ... */ }

    [HttpPost("transfer")]
    [Idempotent(IdempotencyMode.Lock)]
    public async Task<IActionResult> Transfer(TransferRequest request) { /* ... */ }
}
```

> `IdempotentAttribute` 可作用于 `Class` 或 `Method`（`AttributeTargets.Class | AttributeTargets.Method`），并通过 `Inherited = true` 继承。

---

## 6. 主流程（中间件处理逻辑）

`IdempotencyMiddleware.InvokeAsync` 的分支顺序：

1. **读取元数据**：无 `IdempotentAttribute` → 直接放行后续管道，不做幂等处理。
2. **读取幂等键**：
   - 缺头且 `RequireKey = false` → 直接执行；
   - 缺头且 `RequireKey = true` → `400 Bad Request`；
   - 头值非法/过长 → `400 Bad Request`。
3. **计算指纹**：请求体超过 `MaxRequestBodyBytes` → `413 Payload Too Large`。
4. **获取租约** `store.TryAcquireAsync(...)`：
   - `Acquired` → 执行后续管道；
   - `Completed` → 进入“已完成”分支；
   - `InProgress` 且为 `Replay` 模式 → 轮询等待首请求完成（不超过 `ReplayWaitTimeout`），成功则重放，否则 `409`；
   - `FingerprintMismatch` → `409` 冲突；
   - `InProgress`（Reject 模式或等待超时）→ `409` 且带 `Retry-After: 1`。
5. **执行中（Acquired）**：
   - 响应非 2xx **或** `Lock` 模式成功 → 放弃租约（相同请求可再次执行）；
   - `Replay`/`Reject` 成功 → 保存完成记录（`Replay` 捕获响应体以便重放，`Reject` 只记录状态不保存体）；
   - 业务抛异常 → 放弃租约并向上抛出。
6. **已完成（Completed）**：
   - `Replay` 且响应可重放 → 写回原状态码/Content-Type/体，并附加 `Idempotency-Replayed: true`；
   - 其余 → `409` 冲突（重复请求）。

---

## 7. 模式对比

| 模式 | 首次请求 | 重复请求（已完成） | 并发（进行中） | 典型场景 |
| --- | --- | --- | --- | --- |
| `Replay` | 执行业务并保存响应体 | **重放**首次成功响应（含 `Idempotency-Replayed: true`） | 轮询等待后重放 | 支付、下单：客户端希望拿到相同结果 |
| `Reject` | 执行业务并仅记录完成标记 | `409` 冲突（不重放体） | `409` | 仅需“一次成功”，重复直接拒绝 |
| `Lock` | 执行期间持有锁，成功后释放 | 可再次执行（锁已释放） | `409` | 限时互斥，完成后允许重做 |

---

## 8. 响应与状态码速查

| 状态码 | 场景 | 附加头 |
| --- | --- | --- |
| `2xx` | 首次成功执行 | — |
| `2xx` + `Idempotency-Replayed: true` | Replay 模式重放 | `Idempotency-Replayed` |
| `400` | 缺失/无效幂等键（`RequireKey` 或未通过校验） | — |
| `409` | 幂等键冲突（指纹不符）/ 重复请求（Reject）/ 正在处理中 | 进行中带 `Retry-After: 1` |
| `413` | 请求体超过 `MaxRequestBodyBytes` | — |

---

## 9. 范围与后续

- 本组件**仅提供**幂等基础能力（属性、中间件、Redis 存储、配置、指纹与租约），不含任何业务端点接线。
- 业务服务接入需满足：①已调用 `AddIdempotency` / `UseIdempotency`；②在目标端点声明 `IdempotentAttribute`；③前端/网关按约定携带 `Idempotency-Key`。
- 前端接入时不应在多个客户端散落 `localhost` 回退地址；幂等头名称统一来自 `IdempotencyOptions.HeaderName`。

---

## 10. 单元测试

`BuildingBlocks.Idempotency.Tests` 使用 xUnit 覆盖主流程，包含（节选）：

- 无元数据端点直接放行；
- 缺失必填键返回 `400`；
- `Acquired` + `Replay` 执行并捕获可重放响应；
- `Completed` + `Replay` 重放且不执行端点；
- `Completed` + `Reject` 返回 `409`；
- 指纹不匹配返回 `409`；
- Replay 等待并发首请求完成后重放；
- 业务异常放弃租约；
- 请求体/响应体超限分别返回 `413` 与标记不可重放；
- 存储键按用户隔离、指纹包含请求体。

运行：

```bash
dotnet test src/BuildingBlocks/BuildingBlocks.Idempotency.Tests
```
