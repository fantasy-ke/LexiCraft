var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LexiCraft_Host>("webapi");

builder.Build().Run();