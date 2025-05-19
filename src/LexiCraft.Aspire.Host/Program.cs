var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddConnectionString("postgres", "ConnectionStrings:Default");
var redis = builder.AddConnectionString("redis", "ConnectionStrings:Redis");

builder.AddProject<Projects.LexiCraft_AuthServer_Api>("webapi")
    .WithReference(postgres)
    .WithReference(redis);

builder.Build().Run();