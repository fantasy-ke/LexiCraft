var builder = DistributedApplication.CreateBuilder(args);
var postgresIdentity = builder.AddConnectionString("postgres-identity", "ConnectionStrings:postgres-identity");
var postgresVocabulary = builder.AddConnectionString("postgres-vocabulary", "ConnectionStrings:postgres-vocabulary");
var mongoPractice = builder.AddConnectionString("mongo-practice", "ConnectionStrings:mongo-practice");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:redis");

builder.AddProject<Projects.LexiCraft_Services_Identity_Api>("lexicraft-identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresIdentity)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Services_Vocabulary_Api>("lexicraft-vocabulary-api")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresVocabulary)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Services_Practice_Api>("lexicraft-practice-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mongoPractice)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Files_Grpc>("lexicraft-files-grpc")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.LexiCraft_ApiGateway>("lexicraft-api-gateway")
    .WithHttpHealthCheck("/health");

builder.Build().Run();