using BuildingBlocks.Caching.Extensions;
using BuildingBlocks.EventBus.Extensions;
using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using BuildingBlocks.SerilogLogging.Extensions;
using LexiCraft.Services.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.AddServiceDefaults();

builder.Services
    .ConfigureJson()
    .WithCaptcha(builder.Configuration)
    .AddCaching(builder.Configuration);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.AddZEventBus();

builder.AddApplicationServices();

var app = builder.Build();

// 异常处理中间件必须在最前面
app.UseExceptionHandler(_ => { });

app.MapDefaultEndpoints();
app.UseApplication();

if (app.Environment.IsDevelopment()) app.UseAspnetOpenApi();

app.UseHttpsRedirection();

await app.RunAsync();