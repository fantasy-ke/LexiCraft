using BuildingBlocks.Cors;
using BuildingBlocks.SerilogLogging.Extensions;
using LexiCraft.ApiGateway.Extensions;
using LexiCraft.ApiGateway.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 添加Serilog日志
builder.AddSerilogLogging();

// 添加服务默认配置(遥测、健康检查等)
builder.AddServiceDefaults();

// 添加速率限制
builder.Services.AddCustomRateLimiting(builder.Configuration);

// 添加安全头
builder.Services.AddSecurityHeaders(builder.Configuration);

// 添加YARP反向代理
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 添加CORS支持
builder.AddDefaultCors();

var app = builder.Build();

// 配置HTTP请求管道
app.UseDefaultCors();

// 使用安全头
app.UseSecurityHeaders();

// 添加限流中间件
app.UseRateLimiter();

// 添加限流信息传递中间件，在请求转发到后端服务前添加限流信息
app.UseMiddleware<RateLimitInfoMiddleware>();

// 映射默认端点(健康检查等)
app.MapDefaultEndpoints();

// 映射反向代理
app.MapReverseProxy();

app.Run();