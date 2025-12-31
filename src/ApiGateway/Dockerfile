FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ApiGateway/LexiCraft.ApiGateway/LexiCraft.ApiGateway.csproj", "ApiGateway/LexiCraft.ApiGateway/"]
COPY ["LexiCraft.Aspire.ServiceDefaults/LexiCraft.Aspire.ServiceDefaults.csproj", "LexiCraft.Aspire.ServiceDefaults/"]
COPY ["BuildingBlocks/BuildingBlocks.SerilogLogging/BuildingBlocks.SerilogLogging.csproj", "BuildingBlocks/BuildingBlocks.SerilogLogging/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
RUN dotnet restore "ApiGateway/LexiCraft.ApiGateway/LexiCraft.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ApiGateway/LexiCraft.ApiGateway"
RUN dotnet build "./LexiCraft.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LexiCraft.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexiCraft.ApiGateway.dll"]
