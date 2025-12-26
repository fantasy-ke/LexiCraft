using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using LexiCraft.Services.Identity;
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
    .ConfigureJson()
    .WithRedis(builder.Configuration);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddLocalEventBus();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseApplication();

if (app.Environment.IsDevelopment())
{
    app.UseAspnetOpenApi();
}

app.UseHttpsRedirection();

await app.RunAsync();