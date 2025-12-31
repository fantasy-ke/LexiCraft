var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddConnectionString("postgres", "ConnectionStrings:Default");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:Redis");

builder.AddProject<Projects.LexiCraft_Services_Identity_Api>("lexicraft-identity-api")
    .WithReference(postgres)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Files_Grpc>("lexicraft-files-grpc");

builder.AddProject<Projects.LexiCraft_ApiGateway>("lexicraft-api-gateway");

builder.Build().Run();