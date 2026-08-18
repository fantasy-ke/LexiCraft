# BuildingBlocks.OpenApi

`BuildingBlocks.OpenApi` 是 本平台微服务的 OpenAPI 与 API 版本化基础类库，统一提供：

- ASP.NET Core Minimal API 版本管理；
- 按 API 版本生成独立 OpenAPI 文档；
- Scalar API 文档界面；
- OpenAPI 基础信息、Bearer 安全方案和枚举描述转换；
- URL、查询参数和请求头三种 API 版本读取方式。

当前项目面向 `.NET 10`，包版本由仓库根级的 `src/Directory.Packages.props` 集中管理。

## 目录结构

```text
BuildingBlocks.OpenApi/
├── AspnetOpenApi/
│   ├── Extensions/
│   │   ├── DependencyInjectionExtensions.cs
│   │   └── WebApplicationExtensions.cs
│   └── Transformers/
│       ├── BearerSecuritySchemeTransformer.cs
│       ├── EnumSchemaTransformer.cs
│       └── OpenApiVersioningDocumentTransformer.cs
├── BuildingBlocks.OpenApi.csproj
└── OpenApiOptions.cs
```

| 组件 | 职责 |
| --- | --- |
| `AddAspnetOpenApi()` | 注册 API Versioning、ApiExplorer、OpenAPI 和文档转换器 |
| `UseAspnetOpenApi()` | 映射分版本 OpenAPI JSON，并在 Development 环境映射 Scalar |
| `OpenApiVersioningDocumentTransformer` | 根据文档版本填充标题、描述、作者、许可证和版本号 |
| `BearerSecuritySchemeTransformer` | 检测 Bearer 认证方案并写入 OpenAPI 安全定义 |
| `EnumSchemaTransformer` | 将枚举值与 `DescriptionAttribute` 描述组合到 Schema 中 |

## 快速接入

### 1. 引用项目

服务项目需要引用本类库：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\BuildingBlocks.OpenApi\BuildingBlocks.OpenApi.csproj" />
</ItemGroup>
```

实际相对路径应根据调用项目所在目录调整。

### 2. 配置文档信息

`AddAspnetOpenApi()` 默认从配置节 `OpenApiOptions` 绑定 `OpenApiOptions`：

```json
{
  "OpenApiOptions": {
    "Title": "Fantasy Identity API",
    "Description": "Fantasy 身份认证与用户管理服务",
    "AuthorName": "Fantasy",
    "AuthorUrl": "https://example.com",
    "AuthorEmail": "team@example.com",
    "LicenseName": "MIT",
    "LicenseUrl": "https://opensource.org/licenses/MIT"
  }
}
```

支持的配置字段如下：

| 字段 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Title` | `string?` | `null` | OpenAPI 文档标题 |
| `Description` | `string?` | `null` | OpenAPI 文档描述 |
| `AuthorName` | `string?` | `null` | 联系人名称 |
| `AuthorUrl` | `Uri?` | `null` | 联系人地址 |
| `AuthorEmail` | `string?` | `null` | 联系人邮箱 |
| `LicenseName` | `string` | `MIT` | 许可证名称 |
| `LicenseUrl` | `Uri` | MIT 官网 | 许可证地址 |

配置绑定以 `OpenApiOptions` 类的属性为准。配置节中的其他字段不会被该类库读取。

### 3. 注册服务

在服务的基础设施注册方法中调用：

```csharp
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;

builder.Services.AddEndpointsApiExplorer();
builder.AddAspnetOpenApi();
```

当前注册默认配置如下：

- 默认 API 版本：`1.0`；
- 响应中报告支持和已弃用的 API 版本；
- 文档分组格式：`v1`、`v1.1`、`v2`；
- 将路由中的 `{version:apiVersion}` 替换为实际版本；
- 支持从 URL、查询参数和 `api-version` 请求头读取版本。

### 4. 映射 OpenAPI 与 Scalar

在端点映射完成后调用：

```csharp
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;

app.UseApplication();

if (app.Environment.IsDevelopment())
{
    app.UseAspnetOpenApi();
}
```

当前 Identity、Practice 和 Vocabulary 服务只在 Development 环境调用该方法，因此生产环境不会自动暴露 Scalar 或 OpenAPI 端点。若需要在其他环境开放，必须同时评估网关路由、认证、CORS 和信息暴露风险。

## 定义版本化端点

### 路由约定

本平台当前采用 URL 版本段作为稳定契约：

```text
api/v{version:apiVersion}/identity
api/v{version:apiVersion}/vocabulary
api/v{version:apiVersion}/practice
```

例如：

```text
POST /api/v1/identity/login
POST /api/v2/identity/login
GET  /api/v1/vocabulary/words
```

底层虽然同时支持查询参数和请求头版本，但当前路由必须包含版本段。调用方应优先使用 URL 版本，不要在同一个请求中混用互相冲突的 URL、查询参数和请求头版本。

### 单版本端点组

```csharp
using BuildingBlocks.Filters;

var wordsVersionGroup = endpoints
    .NewVersionedApi("Words")
    .WithTags("Words");

var wordsGroupV1 = wordsVersionGroup
    .MapGroup("api/v{version:apiVersion}/vocabulary")
    .HasApiVersion(1.0)
    .WithResultDto();

wordsGroupV1.MapGet("words", Handle)
    .WithName("GetWordsV1");
```

该端点只属于 v1，只会出现在 `v1` OpenAPI 文档中。

可在业务 API 根路由组统一调用 `WithResultDto()`，单个文件流、内部服务契约或自行返回 `IResult` 的端点使用 `WithoutResultDto()` 关闭自动包装。端点过滤器只负责运行时响应，公开接口仍应通过 `Produces<ResultDto<T>>()` 或等价响应元数据声明真实 OpenAPI 契约。

### 不同版本使用不同实现

当请求、响应或业务语义发生破坏性变化时，应为不同版本使用独立 Handler、DTO 和端点名称：

```csharp
var identityVersionGroup = endpoints
    .NewVersionedApi("Identity")
    .WithTags("Identity");

var identityGroupV1 = identityVersionGroup
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasApiVersion(1.0)
    .WithResultDto();

var identityGroupV2 = identityVersionGroup
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasApiVersion(2.0)
    .WithResultDto();

identityGroupV1.MapPost("login", HandleLoginV1)
    .WithName("LoginV1");

identityGroupV2.MapPost("login", HandleLoginV2)
    .WithName("LoginV2");
```

对应关系：

| 请求 | Handler | OpenAPI 文档 |
| --- | --- | --- |
| `POST /api/v1/identity/login` | `HandleLoginV1` | `/openapi/v1.json` |
| `POST /api/v2/identity/login` | `HandleLoginV2` | `/openapi/v2.json` |

端点的 `WithName()` 应保持全局唯一，建议显式包含版本后缀。

### 同一实现支持多个版本

如果两个版本的协议和行为完全一致，可以声明一个支持多个版本的组，并把同一个端点映射到多个版本：

```csharp
var identityGroup = endpoints
    .NewVersionedApi("Identity")
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasApiVersion(1.0)
    .HasApiVersion(2.0)
    .WithResultDto();

identityGroup.MapPost("logout", HandleLogout)
    .WithName("Logout")
    .MapToApiVersion(1.0)
    .MapToApiVersion(2.0);
```

该端点会同时出现在 v1 和 v2 文档中，并接受：

```text
POST /api/v1/identity/logout
POST /api/v2/identity/logout
```

### 同一路径按版本选择不同 Handler

也可以在同一个版本组中注册相同方法和路径，再使用 `MapToApiVersion()` 精确映射：

```csharp
var identityGroup = endpoints
    .NewVersionedApi("Identity")
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasApiVersion(1.0)
    .HasApiVersion(2.0);

identityGroup.MapPost("login", HandleLoginV1)
    .WithName("LoginV1")
    .MapToApiVersion(1.0);

identityGroup.MapPost("login", HandleLoginV2)
    .WithName("LoginV2")
    .MapToApiVersion(2.0);
```

版本系统会使用请求中的 API 版本选择对应端点。

### 弃用旧版本

需要将旧版本标记为弃用时，可以使用 `HasDeprecatedApiVersion()`：

```csharp
var identityGroupV1 = identityVersionGroup
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasDeprecatedApiVersion(1.0);

var identityGroupV2 = identityVersionGroup
    .MapGroup("api/v{version:apiVersion}/identity")
    .HasApiVersion(2.0);
```

由于已启用 `ReportApiVersions`，响应会报告支持版本和已弃用版本。类库还为 `0.9` 配置了 Sunset 策略；若要废弃其他版本，需要同步调整 `DependencyInjectionExtensions.cs` 中的版本策略。

## OpenAPI 文档与 Scalar

`WithDocumentPerVersion()` 根据 ApiExplorer 发现的版本动态创建文档，不需要维护 `["v1", "v2"]` 之类的静态版本数组。

默认文档地址遵循：

```text
/openapi/{groupName}.json
```

例如：

```text
/openapi/v1.json
/openapi/v1.1.json
/openapi/v2.json
```

Scalar 使用 `app.DescribeApiVersions()` 枚举文档，并把每个版本对应到上述 OpenAPI JSON 地址。

> 只声明 `HasApiVersion(2.0)`，但不向该组映射任何端点，不会让 v1 端点自动出现在 v2 文档中。每个需要支持 v2 的端点都必须显式映射或使用 `MapToApiVersion(2.0)`。

## 文档转换器行为

### OpenApiVersioningDocumentTransformer

对每个版本文档执行以下操作：

- 根据 `context.DocumentName` 查找对应的 API 版本描述；
- 设置 `document.Info.Version`；
- 从 `OpenApiOptions` 设置标题、描述、联系人和许可证。

### BearerSecuritySchemeTransformer

当应用已注册名为 `Bearer` 的认证方案时：

- 添加 HTTP Bearer Security Scheme；
- 将 Bearer Security Requirement 添加到文档中的所有操作；
- Scalar 默认选择 `Bearer` 方案。

当前转换器不会根据 `AllowAnonymous()` 排除匿名端点，因此生成文档可能仍在匿名端点上显示 Bearer 输入框。这是文档展示行为，不会改变 ASP.NET Core 实际授权结果。

### EnumSchemaTransformer

当 Schema 对应枚举类型时，将枚举值转换为：

```text
枚举值 - DescriptionAttribute 描述
```

枚举没有描述时，结果取决于项目中的 `GetDescription()` 扩展实现。

## 当前使用方

目前以下服务已调用 `AddAspnetOpenApi()`：

- Identity；
- Vocabulary；
- Practice。

三个服务的端点均使用 `api/v{version:apiVersion}/...` 路由前缀。

## 常见问题

### 新增了 v2 Group，但 v2 文档没有业务端点

原因通常是只创建了：

```csharp
.HasApiVersion(2.0)
```

但没有执行：

```csharp
identityGroupV2.MapLoginV2Endpoint();
```

或没有在共享端点上调用：

```csharp
.MapToApiVersion(2.0)
```

### 请求没有匹配到端点

检查：

1. 路由是否包含 `{version:apiVersion}`；
2. 请求 URL 是否提供了对应版本段；
3. Group 是否通过 `HasApiVersion()` 声明该版本；
4. Endpoint 是否通过 Group 继承或通过 `MapToApiVersion()` 映射该版本；
5. URL、查询参数和请求头中的版本是否冲突。

### OpenAPI 文档没有出现

检查：

1. 是否调用了 `builder.AddAspnetOpenApi()`；
2. 是否在端点映射后调用了 `app.UseAspnetOpenApi()`；
3. 当前环境是否满足调用条件；
4. 至少有一个端点是否映射到目标版本；
5. 是否通过网关访问但缺少对应转发路由。

### 两个版本出现重复端点名问题

同一路径不同版本仍应使用不同端点名称：

```csharp
.WithName("LoginV1")
.WithName("LoginV2")
```

不要为两个独立端点重复使用相同的 `WithName()`。

## 依赖维护注意事项

- 所有包版本统一在 `src/Directory.Packages.props` 中维护；
- `Asp.Versioning.OpenApi` 当前要求 `Microsoft.OpenApi < 3.0.0`；
- 不要单独把 `Microsoft.OpenApi` 升级到 3.x；
- 修改 Asp.Versioning、Microsoft.OpenApi 或 Scalar 后，至少验证 OpenAPI 项目、三个业务服务和活动解决方案构建。

## 验证

类库定向构建：

```powershell
dotnet build src\BuildingBlocks\BuildingBlocks.OpenApi\BuildingBlocks.OpenApi.csproj
```

活动解决方案构建：

```powershell
dotnet build src\Fantasy.slnx
```

运行服务后检查每个实际版本文档，例如：

```powershell
Invoke-WebRequest https://localhost:<port>/openapi/v1.json
Invoke-WebRequest https://localhost:<port>/openapi/v2.json
```

构建通过只能证明编译和依赖解析成功；版本路由、文档内容、Bearer 展示和网关转发仍需要通过运行中的服务验证。
