using System.Diagnostics;
using System.Text.Json;
using LexiCraft.Application.Contract.Events;
using LexiCraft.Application.Contract.Middleware.Dto;
using LexiCraft.Infrastructure.Exceptions;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Infrastructure.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Z.EventBus;

namespace LexiCraft.Application.Contract.Middleware;

/// <summary>
/// 异常中间件
/// </summary>
public class LoginExceptionMiddleware(ILogger<LoginExceptionMiddleware> logger,
    IEventBus<LoginEto> loginEventBus, 
    IOptions<JsonOptions> options) : IMiddleware
{

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await next(context);
            stopwatch.Stop();
            logger.LogInformation($"{context.Request.Path}--duration: {stopwatch.ElapsedMilliseconds} 毫秒");
        }
        catch (AuthLoginException ex)
        {
            context!.Response.StatusCode = 499;
            context.Response.ContentType = "application/json";
            var exDto = JsonSerializer.Deserialize<ExceptionLoginDto>(ex.Message);
            await loginEventBus.PublishAsync(new LoginEto(null, exDto?.UserAccount, null, DateTime.Now,
                context.GetRequestProperty("Origin"), context.GetRequestIp(),
                context.GetRequestProperty("User-Agent"), exDto?.LoginType, false, exDto?.Message));
            
            var response = ResultDto.Fail(exDto.Message, 499);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            logger.LogError($"Middleware AuthLoginException Error:{ex.Message}");
        }
    }
}