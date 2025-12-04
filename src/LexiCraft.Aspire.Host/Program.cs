var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddConnectionString("postgres", "ConnectionStrings:Default");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:Redis");

builder.AddProject<Projects.LexiCraft_AuthServer_Api>("lexicraft-authserver-api")
    .WithReference(postgres)
    .WithReference(redis);

builder.AddProject<Projects.LexiCraft_Files_Grpc>("lexicraft-files-grpc");

builder.Build().Run();