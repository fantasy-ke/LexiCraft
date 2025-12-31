FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["microservices/Files/LexiCraft.Files.Grpc/LexiCraft.Files.Grpc.csproj", "microservices/Files/LexiCraft.Files.Grpc/"]
RUN dotnet restore "microservices/Files/LexiCraft.Files.Grpc/LexiCraft.Files.Grpc.csproj"
COPY . .
WORKDIR "/src/microservices/Files/LexiCraft.Files.Grpc"
RUN dotnet build "./LexiCraft.Files.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LexiCraft.Files.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexiCraft.Files.Grpc.dll"]
