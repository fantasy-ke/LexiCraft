using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgresIdentity = builder.AddConnectionString("postgres-identity", "ConnectionStrings:postgres-identity");
var postgresVocabulary = builder.AddConnectionString("postgres-vocabulary", "ConnectionStrings:postgres-vocabulary");
var mongoPractice = builder.AddConnectionString("mongo-practice", "ConnectionStrings:mongo-practice");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:redis");
var useLocalAgileConfig = builder.Configuration.GetValue<bool>("AgileConfig:UseLocalAgileConfig");
var agileConfigSecurityKey = builder.Configuration["AgileConfig:JwtSecurityKey"];

IResourceBuilder<IResourceWithEndpoints>? agileConfig = null;

if (useLocalAgileConfig)
{
    if (string.IsNullOrWhiteSpace(agileConfigSecurityKey))
    {
        throw new InvalidOperationException(
            "启用本地 AgileConfig 时必须通过 User Secrets 或环境变量配置 AgileConfig:JwtSecurityKey。");
    }

    var postgresAgileConfig =
        builder.AddConnectionString("postgres-agileconfig", "ConnectionStrings:postgres-agileconfig");
    agileConfig = builder.AddContainer("agileconfig", "kklldog/agile_config")
        .WithHttpEndpoint(8000, 5000, "http")
        .WithEnvironment("adminConsole", "true")
        .WithEnvironment("db__provider", "npgsql")
        .WithEnvironment("JwtSetting__SecurityKey", agileConfigSecurityKey)
        .WithEnvironment("TZ", "Asia/Shanghai")
        .WithReference(postgresAgileConfig)
        .WithEnvironment("db__conn", postgresAgileConfig)
        .WithLifetime(ContainerLifetime.Persistent);
}


var identityApi = builder.AddProject<Fantasy_Services_Identity_Api>("fantasy-identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresIdentity)
    .WithReference(redis)
    .WithAgileConfig(agileConfig);

var vocabularyApi = builder.AddProject<LexiCraft_Services_Vocabulary_Api>("lexicraft-vocabulary-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresVocabulary)
    .WithReference(redis)
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithEnvironment("PermissionAuthorizationOptions__IdentityApiBaseAddress", identityApi.GetEndpoint("http"))
    .WithAgileConfig(agileConfig);

var practiceApi = builder.AddProject<LexiCraft_Services_Practice_Api>("lexicraft-practice-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongoPractice)
    .WithReference(redis)
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithEnvironment("PermissionAuthorizationOptions__IdentityApiBaseAddress", identityApi.GetEndpoint("http"))
    .WithAgileConfig(agileConfig);

var filesGrpc = builder.AddProject<Fantasy_Files_Grpc>("fantasy-files-grpc")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithEnvironment("PermissionAuthorizationOptions__IdentityApiBaseAddress", identityApi.GetEndpoint("http"))
    .WithAgileConfig(agileConfig);

var apiGateway = builder.AddProject<Fantasy_ApiGateway>("fantasy-api-gateway")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(identityApi)
    .WaitFor(vocabularyApi)
    .WaitFor(practiceApi)
    .WaitFor(filesGrpc)
    .WithAgileConfig(agileConfig);

builder.Build().Run();

public static class AspireExtensions
{
    public static IResourceBuilder<T> WithAgileConfig<T>(this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithEndpoints>? agileConfig = null)
        where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        var serviceConfig = builder.ApplicationBuilder.Configuration.GetSection("AgileConfig")
            .GetSection(builder.Resource.Name);

        var nodes = agileConfig?.GetEndpoint("http").ToString() ?? serviceConfig["Nodes"];
        var appId = serviceConfig["AppId"];
        var secret = serviceConfig["Secret"];
        var environment = serviceConfig["Env"];

        if (string.IsNullOrWhiteSpace(nodes) || string.IsNullOrWhiteSpace(appId) ||
            string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(environment))
        {
            throw new InvalidOperationException(
                $"缺少 {builder.Resource.Name} 的 AgileConfig 配置。请通过 User Secrets 或环境变量提供 Nodes、AppId、Secret 和 Env。");
        }

        builder.WithEnvironment("AgileConfig__Nodes", nodes)
            .WithEnvironment("AgileConfig__AppId", appId)
            .WithEnvironment("AgileConfig__Secret", secret)
            .WithEnvironment("AgileConfig__ENV", environment);

        if (agileConfig != null) builder.WaitFor(agileConfig);

        return builder;
    }
}