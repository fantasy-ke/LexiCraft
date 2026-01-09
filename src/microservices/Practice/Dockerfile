FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["microservices/Practice/LexiCraft.Services.Practice.Api/LexiCraft.Services.Practice.Api.csproj", "microservices/Practice/LexiCraft.Services.Practice.Api/"]
COPY ["microservices/Practice/LexiCraft.Services.Practice/LexiCraft.Services.Practice.csproj", "microservices/Practice/LexiCraft.Services.Practice/"]
COPY ["BuildingBlocks/BuildingBlocks.MongoDB/BuildingBlocks.MongoDB.csproj", "BuildingBlocks/BuildingBlocks.MongoDB/"]
COPY ["BuildingBlocks/BuildingBlocks.Authorization/BuildingBlocks.Authorization.csproj", "BuildingBlocks/BuildingBlocks.Authorization/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["BuildingBlocks/BuildingBlocks.Caching/BuildingBlocks.Caching.csproj", "BuildingBlocks/BuildingBlocks.Caching/"]
COPY ["BuildingBlocks/BuildingBlocks.Grpc.Contracts/BuildingBlocks.Grpc.Contracts.csproj", "BuildingBlocks/BuildingBlocks.Grpc.Contracts/"]
COPY ["BuildingBlocks/BuildingBlocks.EventBus/BuildingBlocks.EventBus.csproj", "BuildingBlocks/BuildingBlocks.EventBus/"]
COPY ["BuildingBlocks/BuildingBlocks.OpenApi/BuildingBlocks.OpenApi.csproj", "BuildingBlocks/BuildingBlocks.OpenApi/"]
COPY ["BuildingBlocks/BuildingBlocks.Validation/BuildingBlocks.Validation.csproj", "BuildingBlocks/BuildingBlocks.Validation/"]
COPY ["BuildingBlocks/BuildingBlocks.SerilogLogging/BuildingBlocks.SerilogLogging.csproj", "BuildingBlocks/BuildingBlocks.SerilogLogging/"]
COPY ["microservices/Shared/LexiCraft.Shared/LexiCraft.Shared.csproj", "microservices/Shared/LexiCraft.Shared/"]
COPY ["LexiCraft.Aspire.ServiceDefaults/LexiCraft.Aspire.ServiceDefaults.csproj", "LexiCraft.Aspire.ServiceDefaults/"]
RUN dotnet restore "microservices/Practice/LexiCraft.Services.Practice.Api/LexiCraft.Services.Practice.Api.csproj"
COPY . .
WORKDIR "/src/microservices/Practice/LexiCraft.Services.Practice.Api"
RUN dotnet build "./LexiCraft.Services.Practice.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LexiCraft.Services.Practice.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexiCraft.Services.Practice.Api.dll"]