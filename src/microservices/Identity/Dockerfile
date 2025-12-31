FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["microservices/LexiCraft.Services.Identity.Api/LexiCraft.Services.Identity.Api.csproj", "microservices/LexiCraft.Services.Identity.Api/"]
COPY ["microservices/LexiCraft.Services.Identity/LexiCraft.Services.Identity.csproj", "microservices/LexiCraft.Services.Identity/"]
COPY ["BuildingBlocks/BuildingBlocks.EntityFrameworkCore.Postgres/BuildingBlocks.EntityFrameworkCore.Postgres.csproj", "BuildingBlocks/BuildingBlocks.EntityFrameworkCore.Postgres/"]
COPY ["BuildingBlocks/BuildingBlocks.EntityFrameworkCore/BuildingBlocks.EntityFrameworkCore.csproj", "BuildingBlocks/BuildingBlocks.EntityFrameworkCore/"]
COPY ["BuildingBlocks/BuildingBlocks.Authorization/BuildingBlocks.Authorization.csproj", "BuildingBlocks/BuildingBlocks.Authorization/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["BuildingBlocks/BuildingBlocks.Caching/BuildingBlocks.Caching.csproj", "BuildingBlocks/BuildingBlocks.Caching/"]
COPY ["BuildingBlocks/BuildingBlocks.Grpc.Contracts/BuildingBlocks.Grpc.Contracts.csproj", "BuildingBlocks/BuildingBlocks.Grpc.Contracts/"]
COPY ["framework/Z.EventBus/Z.EventBus.csproj", "framework/Z.EventBus/"]
COPY ["BuildingBlocks/BuildingBlocks.OpenApi/BuildingBlocks.OpenApi.csproj", "BuildingBlocks/BuildingBlocks.OpenApi/"]
COPY ["BuildingBlocks/BuildingBlocks.Validation/BuildingBlocks.Validation.csproj", "BuildingBlocks/BuildingBlocks.Validation/"]
COPY ["BuildingBlocks/BuildingBlocks.SerilogLogging/BuildingBlocks.SerilogLogging.csproj", "BuildingBlocks/BuildingBlocks.SerilogLogging/"]
COPY ["framework/Z.Local.EventBus/Z.Local.EventBus.csproj", "framework/Z.Local.EventBus/"]
COPY ["LexiCraft.Aspire.ServiceDefaults/LexiCraft.Aspire.ServiceDefaults.csproj", "LexiCraft.Aspire.ServiceDefaults/"]
RUN dotnet restore "microservices/LexiCraft.Services.Identity.Api/LexiCraft.Services.Identity.Api.csproj"
COPY . .
WORKDIR "/src/microservices/LexiCraft.Services.Identity.Api"
RUN dotnet build "./LexiCraft.Services.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LexiCraft.Services.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexiCraft.Services.Identity.Api.dll"]
