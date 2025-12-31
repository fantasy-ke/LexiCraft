# LexiCraft

LexiCraft 是一个基于 .NET 的项目，围绕微服务架构构建，并使用 .NET Aspire 进行编排。它旨在通过清晰的职责分离来构建可扩展、可维护的应用程序。

## 项目结构

项目分为几个关键领域：

- **`src/`**: 主要源代码目录。
  - **`LexiCraft.Aspire.Host/`**: .NET Aspire 主机项目，用于编排应用程序的各种服务和组件。
  - **`LexiCraft.Aspire.ServiceDefaults/`**: 包含 Aspire 生态系统的默认服务配置和扩展。
  - **`microservices/`**: 存放应用程序的不同微服务。
    - **`LexiCraft.Services.Identity/`**: 用户身份认证和管理服务，包含用户、权限、登录等核心功能。
    - **`LexiCraft.Services.Identity.Api/`**: 用户身份认证服务的API入口点。
    - **`LexiCraft.Files.Grpc/`**: 文件管理服务，通过 gRPC 提供文件上传、下载等功能。
  - **`BuildingBlocks/`**: 包含跨不同服务使用的共享库和组件。这包括常见的功能，例如：
    - **`BuildingBlocks`**: 核心领域逻辑、仓库和异常处理。
    - **`BuildingBlocks.Authorization`**: 授权相关组件，包括 JWT 处理和权限检查。
    - **`BuildingBlocks.Caching`**: 统一缓存管理组件，支持 Redis 等缓存实现。
    - **`BuildingBlocks.EntityFrameworkCore`**: 基于 EF Core 的数据访问组件。
    - **`BuildingBlocks.EntityFrameworkCore.Postgres`**: PostgreSQL 数据库相关组件。
    - **`BuildingBlocks.Grpc.Contracts`**: 用于服务间通信的 gRPC 契约。
    - **`BuildingBlocks.OpenApi`**: OpenAPI (Swagger/Scalar) 相关配置和组件。
    - **`BuildingBlocks.SerilogLogging`**: 基于 Serilog 的统一日志记录组件。
    - **`BuildingBlocks.Validation`**: 请求验证相关组件，基于 FluentValidation。
    - `BuildingBlocks.MongoDB`: MongoDB数据访问组件
  - **`framework/`**: 包含为项目开发的自定义框架和库。
    - **`Z.EventBus`**: 用于异步通信的事件总线定义和抽象。
    - **`Z.Local.EventBus`**: 基于内存或本地机制的事件总线实现。
    - **`Z.OSSCore`**: 用于对象存储服务 (OSS) 跨平台交互的核心组件（支持阿里云、腾讯云、Minio 等）。

## 功能特性

- **身份认证**: 通过 `LexiCraft.Services.Identity` 实现用户注册、登录、权限管理等功能。
- **OAuth 集成**: 支持 GitHub、Gitee 等第三方登录。
- **文件管理**: 通过 gRPC 提供高效的文件上传和管理功能。
- **缓存机制**: 集成 Redis 缓存以提高系统性能。
- **API 版本控制**: 支持 API 版本管理，便于系统演进。
- **CQRS 模式**: 使用 MediatR 实现命令查询职责分离。
- **请求验证**: 使用 FluentValidation 实现请求参数验证。
- **事件驱动**: 通过事件总线实现服务间的异步通信。

## 快速开始

1. **先决条件**：

   - .NET SDK 10.0+ (Preview)
   - Docker（用于运行 Redis、PostgreSQL 等依赖项）
   - Visual Studio 2022 Preview 或 JetBrains Rider (支持 .slnx 格式)
2. **运行应用程序**：

   - 在您首选的 IDE 中打开 `src/LexiCraft.slnx` 解决方案文件。
   - 将 `LexiCraft.Aspire.Host` 设置为启动项目。
   - 运行项目。这将启动 Aspire 仪表板和所有注册的服务。

## 技术栈

- **.NET 10.0**
- **.NET Aspire**（用于编排）
- **ASP.NET Core**（用于 Web API）
- **Entity Framework Core**（用于数据访问）
- **PostgreSQL**（主要数据库）
- **Redis**（用于缓存）
- **gRPC**（用于服务间通信）
- **JWT**（用于认证）
- **Serilog**（用于日志记录）
- **FluentValidation**（用于请求验证）
- **MediatR**（用于 CQRS 模式）
- **Asp.Versioning**（用于 API 版本控制）

## 项目架构

项目采用微服务架构，使用 CQRS 模式分离命令和查询职责。Building Blocks 层提供通用功能，如认证、授权、数据访问等，确保代码复用和一致性。通过 .NET Aspire 进行服务编排，简化了微服务部署和管理。

---

*此 README 是根据项目实际结构生成的。*
