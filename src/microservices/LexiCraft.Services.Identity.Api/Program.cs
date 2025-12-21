
using LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddInfrastructure();

var app = builder.Build();

app.UseInfrastructure();

app.Run();