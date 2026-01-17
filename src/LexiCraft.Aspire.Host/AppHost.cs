using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var postgresIdentity = builder.AddConnectionString("postgres-identity", "ConnectionStrings:postgres-identity");
var postgresVocabulary = builder.AddConnectionString("postgres-vocabulary", "ConnectionStrings:postgres-vocabulary");
var mongoPractice = builder.AddConnectionString("mongo-practice", "ConnectionStrings:mongo-practice");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:redis");
var useLocalAgileConfig = builder.Configuration.GetValue<bool>("AgileConfig:UseLocalAgileConfig");

IResourceBuilder<IResourceWithEndpoints>? agileConfig = null;

if (useLocalAgileConfig)
{
    var postgresAgileConfig = builder.AddConnectionString("postgres-agileconfig", "ConnectionStrings:postgres-agileconfig");
    agileConfig = builder.AddContainer("agileconfig", "kklldog/agile_config")
        .WithHttpEndpoint(port: 8000, targetPort: 5000, name: "http")
        .WithEnvironment("adminConsole", "true")
        .WithEnvironment("db__provider", "npgsql")
        .WithEnvironment("JwtSetting__SecurityKey", "LexiCraft_AgileConfig_Secret_Key")
        .WithEnvironment("TZ", "Asia/Shanghai")
        .WithReference(postgresAgileConfig)
        .WithEnvironment("db__conn", postgresAgileConfig)
        .WithLifetime(ContainerLifetime.Persistent);
}


var identityApi = builder.AddProject<Projects.LexiCraft_Services_Identity_Api>("lexicraft-identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresIdentity)
    .WithReference(redis)
    .WithAgileConfig(agileConfig);

var vocabularyApi = builder.AddProject<Projects.LexiCraft_Services_Vocabulary_Api>("lexicraft-vocabulary-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresVocabulary)
    .WithReference(redis)
    .WithAgileConfig(agileConfig);

var practiceApi = builder.AddProject<Projects.LexiCraft_Services_Practice_Api>("lexicraft-practice-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongoPractice)
    .WithReference(redis)
    .WithAgileConfig(agileConfig);

var filesGrpc = builder.AddProject<Projects.LexiCraft_Files_Grpc>("lexicraft-files-grpc")
    .WithHttpHealthCheck("/health")
    .WithAgileConfig(agileConfig);

var apiGateway = builder.AddProject<Projects.LexiCraft_ApiGateway>("lexicraft-api-gateway")
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
    public static IResourceBuilder<T> WithAgileConfig<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResourceWithEndpoints>? agileConfig = null) 
        where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        var serviceConfig = builder.ApplicationBuilder.Configuration.GetSection("AgileConfig").GetSection(builder.Resource.Name);

        var nodes = agileConfig?.GetEndpoint("http").ToString() ?? serviceConfig.GetValue<string>("Nodes");

        builder.WithEnvironment("AgileConfig__Nodes", nodes)
               .WithEnvironment("AgileConfig__AppId", serviceConfig["AppId"])
               .WithEnvironment("AgileConfig__Secret", serviceConfig["Secret"])
               .WithEnvironment("AgileConfig__ENV", serviceConfig["Env"]);

        if (agileConfig != null)
        {
            builder.WaitFor(agileConfig);
        }

        return builder;
    }
}
