var builder = DistributedApplication.CreateBuilder(args);
var postgresIdentity = builder.AddConnectionString("postgres-identity", "ConnectionStrings:postgres-identity");
var postgresVocabulary = builder.AddConnectionString("postgres-vocabulary", "ConnectionStrings:postgres-vocabulary");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:redis");

builder.AddProject<Projects.LexiCraft_Services_Identity_Api>("lexicraft-identity-api")
    .WithReference(postgresIdentity)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Services_Vocabulary_Api>("lexicraft-vocabulary-api")
    .WithReference(postgresVocabulary)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Files_Grpc>("lexicraft-files-grpc");

builder.AddProject<Projects.LexiCraft_ApiGateway>("lexicraft-api-gateway");

builder.Build().Run();