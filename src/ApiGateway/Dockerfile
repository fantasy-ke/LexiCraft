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
COPY ["ApiGateway/Fantasy.ApiGateway/Fantasy.ApiGateway.csproj", "ApiGateway/Fantasy.ApiGateway/"]
COPY ["Fantasy.Aspire.ServiceDefaults/Fantasy.Aspire.ServiceDefaults.csproj", "Fantasy.Aspire.ServiceDefaults/"]
COPY ["BuildingBlocks/BuildingBlocks.SerilogLogging/BuildingBlocks.SerilogLogging.csproj", "BuildingBlocks/BuildingBlocks.SerilogLogging/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
RUN dotnet restore "ApiGateway/Fantasy.ApiGateway/Fantasy.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ApiGateway/Fantasy.ApiGateway"
RUN dotnet build "./Fantasy.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Fantasy.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fantasy.ApiGateway.dll"]
