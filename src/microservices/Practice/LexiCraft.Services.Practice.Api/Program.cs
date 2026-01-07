using BuildingBlocks.Caching.Extensions;
using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using LexiCraft.Services.Practice;
using BuildingBlocks.SerilogLogging.Extensions;
using BuildingBlocks.EventBus.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.AddServiceDefaults();

builder.Services
    .ConfigureJson()
    .AddRedisCaching();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

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