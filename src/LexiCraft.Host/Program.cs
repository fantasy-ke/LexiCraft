using LexiCraft.Host;
using LexiCraft.Host.RouterMap;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Infrastructure.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services
    .WithScalar(new OpenApiInfo()
    {
        Title = "词汇技艺 Web Api",
        Version = "v1",
        Description = "词汇技艺相关接口",
    })
    .WithJwt(builder.Configuration)
    .WithLexiCraftDbAccess(builder.Configuration)
    .WithRedis(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<ExceptionMiddleware>();

builder.Services.ServicesCors(options =>
{
    options.CorsName = "LexiCraft.Cors";
    options.CorsArr = builder.Configuration["App:CorsOrigins"]!
        .Split(",", StringSplitOptions.RemoveEmptyEntries) //获取移除空白字符串
        .Select(o => o.RemoveFix("/"))
        .ToArray();
});

builder.Services.WithIdGen();

builder.Services.WithServiceLifetime();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors("LexiCraft.Cors");

app.MapAuthEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseScalar("EarthChat Auth Server");
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();


await app.RunAsync();