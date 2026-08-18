FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["microservices/Files/Fantasy.Files.Grpc/Fantasy.Files.Grpc.csproj", "microservices/Files/Fantasy.Files.Grpc/"]
COPY ["BuildingBlocks/BuildingBlocks.Authorization/BuildingBlocks.Authorization.csproj", "BuildingBlocks/BuildingBlocks.Authorization/"]
COPY ["BuildingBlocks/BuildingBlocks.Caching/BuildingBlocks.Caching.csproj", "BuildingBlocks/BuildingBlocks.Caching/"]
COPY ["microservices/Shared/Fantasy.Shared/Fantasy.Shared.csproj", "microservices/Shared/Fantasy.Shared/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["BuildingBlocks/BuildingBlocks.SerilogLogging/BuildingBlocks.SerilogLogging.csproj", "BuildingBlocks/BuildingBlocks.SerilogLogging/"]
COPY ["Fantasy.Aspire.ServiceDefaults/Fantasy.Aspire.ServiceDefaults.csproj", "Fantasy.Aspire.ServiceDefaults/"]
COPY ["BuildingBlocks/BuildingBlocks.Grpc.Contracts/BuildingBlocks.Grpc.Contracts.csproj", "BuildingBlocks/BuildingBlocks.Grpc.Contracts/"]
COPY ["BuildingBlocks/BuildingBlocks.EntityFrameworkCore/BuildingBlocks.EntityFrameworkCore.csproj", "BuildingBlocks/BuildingBlocks.EntityFrameworkCore/"]
COPY ["BuildingBlocks/BuildingBlocks.OpenApi/BuildingBlocks.OpenApi.csproj", "BuildingBlocks/BuildingBlocks.OpenApi/"]
COPY ["BuildingBlocks/BuildingBlocks.OSS/BuildingBlocks.OSS.csproj", "BuildingBlocks/BuildingBlocks.OSS/"]
RUN dotnet restore "microservices/Files/Fantasy.Files.Grpc/Fantasy.Files.Grpc.csproj"
COPY . .
WORKDIR "/src/microservices/Files/Fantasy.Files.Grpc"
RUN dotnet build "./Fantasy.Files.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Fantasy.Files.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fantasy.Files.Grpc.dll"]
