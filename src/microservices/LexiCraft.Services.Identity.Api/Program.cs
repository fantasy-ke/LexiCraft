
using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using LexiCraft.Services.Identity;
using LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;
using Microsoft.OpenApi;
using Serilog;
using Z.Local.EventBus;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(path: "serilog.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.AddServiceDefaults();

builder.Services
    .WithScalar(new OpenApiInfo()
    {
        Title = "词汇技艺 Web Api",
        Version = "v1",
        Description = "词汇技艺相关接口",
    })
    .ConfigureJson()
    .WithRedis(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddLocalEventBus();

builder.AddApplicationServices();

builder.AddInfrastructure();


var app = builder.Build();

app.MapDefaultEndpoints();

app.UseInfrastructure();

app.MapApplicationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseScalar("LexiCraft Auth Server");
}

app.UseHttpsRedirection();

await app.RunAsync();