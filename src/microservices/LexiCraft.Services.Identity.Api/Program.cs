using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using LexiCraft.Services.Identity;
using BuildingBlocks.SerilogLogging.Extensions;
using Serilog;
using Z.Local.EventBus;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.AddServiceDefaults();

builder.Services
    .ConfigureJson()
    .WithCaptcha(builder.Configuration)
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