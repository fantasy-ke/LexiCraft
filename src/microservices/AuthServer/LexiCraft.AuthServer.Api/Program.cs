using BuildingBlocks.Authentication;
using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.Serilog;
using BuildingBlocks.Shared;
using LexiCraft.AuthServer.Api;
using LexiCraft.AuthServer.Application.Contract.Users.Authorization;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Extensions;
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

builder.AddServiceDefaults();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Host.UseSerilog();

builder.Services
    .WithScalar(new OpenApiInfo()
    {
        Title = "词汇技艺 Web Api",
        Version = "v1",
        Description = "词汇技艺相关接口",
    })
    .ConfigureJson()
    .WithJwt(builder.Configuration)
    .WithLexiCraftDbAccess(builder.Configuration)
    .WithRedis(builder.Configuration);

builder.Services.Configure<OAuthOption>(
    builder.Configuration.GetSection("OAuthOptions"));

builder.Services.AddEndpointsApiExplorer();
//Cross-Cutting Services
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
// builder.Services.AddScoped<ExceptionMiddleware>();
// builder.Services.AddScoped<LoginExceptionMiddleware>();

builder.Services.RegisterAuthorization();
builder.Services.AddAuthorization();

// 注册权限定义提供程序
builder.Services.AddPermissionDefinitionProvider<UsersPermissionDefinitionProvider>();


builder.Services.ServicesCors(options =>
{
    options.CorsName = "LexiCraft.Cors";
    options.CorsArr = builder.Configuration["App:CorsOrigins"]!
        .Split(",", StringSplitOptions.RemoveEmptyEntries) //获取移除空白字符串
        .Select(o => o.RemoveFix("/"))
        .ToArray();
});



builder.Services.AddLocalEventBus();

builder.Services.AddCaptcha(builder.Configuration);

builder.Services.AddGrpcService(builder.Configuration);

builder.Services.WithMapster();

builder.Services.WithIdGen();

builder.Services.WithServiceLifetime();
builder.Services.WithFantasyLife();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = SerilogRequestUtility.HttpMessageTemplate;
    options.GetLevel = SerilogRequestUtility.GetRequestLevel;
    options.EnrichDiagnosticContext = SerilogRequestUtility.EnrichFromRequest;
});

app.MapDefaultEndpoints();

app.UseCors("LexiCraft.Cors");

// app.MapAuthEndpoint();

app.MapFantasyApis(options =>
{
    options.Prefix = "v1";                    // API 前缀
    options.Version = "1.0";                  // API 版本
    options.DisableAutoMapRoute = false;      // 禁用自动路由映射
    options.AutoAppendId = true;              // 自动追加 ID 参数
    options.PluralizeServiceName = true;      // 服务名复数化
    options.EnableProperty = false;           // 启用属性访问
    options.DisableTrimMethodPrefix = false;  // 禁用方法前缀修剪
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseScalar("LexiCraft Auth Server");
}
app.UseExceptionHandler(_ => { });
// app.UseMiddleware<ExceptionMiddleware>();
// app.UseMiddleware<LoginExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();


await app.RunAsync();