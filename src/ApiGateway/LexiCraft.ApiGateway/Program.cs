using BuildingBlocks.SerilogLogging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 添加Serilog日志
builder.AddSerilogLogging();

// 添加服务默认配置(遥测、健康检查等)
builder.AddServiceDefaults();

// 添加YARP反向代理
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 添加CORS支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置HTTP请求管道
app.UseCors();

// 映射默认端点(健康检查等)
app.MapDefaultEndpoints();

// 映射反向代理
app.MapReverseProxy();

app.Run();
