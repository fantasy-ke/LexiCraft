using LexiCraft.Host;
using LexiCraft.Host.RouterMap;
using LexiCraft.Infrastructure.Authorization;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Infrastructure.Middleware;
using LexiCraft.Infrastructure.Serilog;
using LexiCraft.Infrastructure.Shared;
using Microsoft.OpenApi.Models;
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
    .WithJwt(builder.Configuration)
    .WithLexiCraftDbAccess(builder.Configuration)
    .WithRedis(builder.Configuration);

builder.Services.Configure<OAuthOption>(configuration.GetSection("OAuthOptions"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<ExceptionMiddleware>();

builder.Services.RegisterAuthorization();
builder.Services.AddAuthorization();

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

builder.Services.WithIdGen();


builder.Services.WithServiceLifetime();

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

app.MapAuthEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseScalar("EarthChat Auth Server");
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();


await app.RunAsync();