# LexiCraft Project Context

## Project Overview

LexiCraft is a .NET-based microservices project built with .NET Aspire for orchestration. It follows a clean architecture approach with clear separation of concerns, designed to build scalable and maintainable applications. The project implements a comprehensive learning platform with features for identity management, vocabulary services, practice modules, and file management.

### Key Technologies
- **.NET 10.0** (with preview support)
- **.NET Aspire** for service orchestration
- **YARP** for API gateway functionality
- **ASP.NET Core** for web APIs
- **Entity Framework Core** for data access
- **PostgreSQL** as primary database
- **MongoDB** for document storage
- **Redis** for caching
- **gRPC** for service-to-service communication
- **JWT** for authentication
- **Serilog** for logging
- **FluentValidation** for request validation
- **MediatR** for CQRS pattern implementation

### Architecture Components

#### Microservices
- **Identity Service** (`LexiCraft.Services.Identity`): Handles user authentication, registration, and authorization
- **Vocabulary Service** (`LexiCraft.Services.Vocabulary`): Manages vocabulary-related functionality
- **Practice Service** (`LexiCraft.Services.Practice`): Implements practice/exercise modules using MongoDB
- **Files Service** (`LexiCraft.Files.Grpc`): Provides file management via gRPC

#### Building Blocks
Shared libraries providing cross-cutting concerns:
- **BuildingBlocks**: Core domain logic, repositories, and exception handling
- **BuildingBlocks.Authorization**: JWT handling and permission checks
- **BuildingBlocks.Caching**: Unified caching with Redis support
- **BuildingBlocks.EntityFrameworkCore**: EF Core data access components
- **BuildingBlocks.EntityFrameworkCore.Postgres**: PostgreSQL-specific components
- **BuildingBlocks.Grpc.Contracts**: gRPC contracts for inter-service communication
- **BuildingBlocks.OpenApi**: OpenAPI/Swagger/Scalar configurations
- **BuildingBlocks.EventBus**: Event bus implementation with local memory and Redis support
- **BuildingBlocks.SerilogLogging**: Unified Serilog-based logging
- **BuildingBlocks.Validation**: Request validation using FluentValidation
- **BuildingBlocks.OSS**: Object Storage Service abstraction supporting Alibaba Cloud, Tencent Cloud, MinIO
- **BuildingBlocks.MongoDB**: MongoDB data access components

#### Infrastructure
- **API Gateway** (`ApiGateway`): Built with YARP for request routing, load balancing, and cross-cutting concerns
- **Aspire Host** (`LexiCraft.Aspire.Host`): Orchestrates all services and manages dependencies
- **Service Defaults** (`LexiCraft.Aspire.ServiceDefaults`): Common configurations for all services

## Building and Running

### Prerequisites
- .NET SDK 10.0+ (Preview)
- Docker (for running Redis, PostgreSQL, and other dependencies)
- Visual Studio 2022 Preview or JetBrains Rider (supports .slnx format)

### Development Setup
1. Open the solution file `src/LexiCraft.slnx` in your preferred IDE
2. Set `LexiCraft.Aspire.Host` as the startup project
3. Run the project to start the Aspire dashboard and all registered services

### Docker Deployment Options
The project supports multiple deployment strategies:

1. **Single-stack Docker Compose** (for development/testing):
   ```bash
   cd src
   docker compose up -d --build
   ```

2. **Blue-Green Deployment** (for zero-downtime deployments):
   - Blue stack: `APIGATEWAY_PORT=5000 docker compose -p lexicraft_blue up -d --build`
   - Green stack: `APIGATEWAY_PORT=5001 docker compose -p lexicraft_green up -d --build`

3. **Docker Swarm** (for multi-node environments):
   - Configure `deploy` sections in `compose.yaml`
   - Use `docker stack deploy -c compose.yaml lexicraft`

4. **Kubernetes** (for production environments):
   - Create Deployments and Services for each microservice
   - Use rolling updates with configurable strategies

### Configuration Management
The project uses AgileConfig for centralized configuration management. Services connect to a configuration server to retrieve environment-specific settings. Connection strings for databases, Redis, and other services are defined in the Aspire appsettings.json file.

## Development Conventions

### Code Organization
- Follows Domain-Driven Design (DDD) principles
- Uses CQRS pattern with MediatR for separating commands and queries
- Implements event-driven architecture with Saga pattern for distributed transactions
- Leverages building blocks for shared functionality to avoid duplication

### Testing Approach
- Unit tests using xUnit framework
- Integration tests with Testcontainers for database testing
- Health checks implemented for all services
- API versioning for backward compatibility

### Logging and Monitoring
- Structured logging with Serilog
- OpenTelemetry integration for observability
- Health check endpoints for service monitoring
- Centralized configuration management

### Security Practices
- JWT-based authentication
- OAuth integration (GitHub, Gitee)
- Input validation using FluentValidation
- Secure connection strings and configuration management

## Project Structure
```
src/
├── ApiGateway/           # YARP-based API Gateway
├── BuildingBlocks/       # Shared libraries and components
├── LexiCraft.Aspire.Host/ # Aspire orchestration
├── LexiCraft.Aspire.ServiceDefaults/ # Common service configurations
├── microservices/        # Individual microservices
│   ├── Identity/         # User identity management
│   ├── Vocabulary/       # Vocabulary services
│   └── Practice/         # Practice/exercise modules
├── UIs/                  # Frontend applications
├── compose.yaml          # Docker Compose configuration
├── Directory.Build.props # Global MSBuild properties
├── Directory.Packages.props # Centralized package management
└── global.json           # .NET SDK version specification
```

## Key Features
- User authentication and OAuth integration
- File management via gRPC services
- Redis caching for improved performance
- API versioning for system evolution
- CQRS pattern with MediatR
- Request validation with FluentValidation
- Event-driven architecture with eventual consistency
- Support for multiple object storage providers (Alibaba Cloud, Tencent Cloud, MinIO)