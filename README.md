# LexiCraft

LexiCraft 是一个基于 .NET 的项目，围绕微服务架构构建，并使用 .NET Aspire 进行编排。它旨在通过清晰的职责分离来构建可扩展、可维护的应用程序。

## 项目结构

项目分为几个关键领域：

- **`src/`**: 主要源代码目录。
  - **`LexiCraft.Aspire.Host/`**: .NET Aspire 主机项目，用于编排应用程序的各种服务和组件。
  - **`LexiCraft.Aspire.ServiceDefaults/`**: 包含 Aspire 生态系统的默认服务配置和扩展。
  - **`Services/`**: 存放应用程序的不同微服务。
    - **`AuthServer/`**: 认证服务，可能处理用户身份和访问控制。
    - **`FliesServer/` (可能拼写错误，可能是 `FilesServer`)**: 文件管理服务，通过 gRPC 公开功能。
  - **`BuildingBlocks/`**: 包含跨不同服务使用的共享库和组件。这包括常见的功​​能，例如：
    - **`BuildingBlocks`**: 核心领域逻辑、仓库和异常处理。
    - **`BuildingBlocks.Authorization`**: 授权相关组件，包括 JWT 处理和权限检查。
    - **`BuildingBlocks.EntityFrameworkCore`**: 基于 EF Core 的数据访问组件。
    - **`BuildingBlocks.Grpc.Contracts`**: 用于服务间通信的 gRPC 契约。
  - **`framework/`**: 包含为项目开发的自定义框架和库。
    - **`Z.EventBus`**: 用于异步通信的事件总线实现。
    - **`Z.FreeRedis`**: Redis 缓存库。
    - **`Z.Local.EventBus`**: 本地事件总线实现。
    - **`Z.OSSCore`**: 用于对象存储服务 (OSS) 交互的组件。

## 快速开始

1.  **先决条件**：
    - .NET SDK（版本取决于项目的目标框架，由于 Aspire，很可能是 .NET 8+）
    - Docker（用于运行 Redis、数据库等依赖项）

2.  **运行应用程序**：
    - 在您首选的 IDE（Visual Studio、JetBrains Rider）中打开 `LexiCraft.sln` 解决方案文件。
    - 将 `LexiCraft.Aspire.Host` 设置为启动项目。
    - 运行项目。这将启动 Aspire 仪表板和所有注册的服务。

## 关键技术

-   **.NET**
-   **.NET Aspire**（用于编排）
-   **ASP.NET Core**（用于 Web API）
-   **Entity Framework Core**（用于数据访问）
-   **gRPC**（用于服务间通信）
-   **Redis**（用于缓存）
-   **JWT**（用于认证）
-   **Serilog**（用于日志记录）

---
*此 README 是根据项目结构自动生成的。*