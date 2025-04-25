using System.Diagnostics;
using System.Text.Json;
using LexiCraft.Infrastructure.Exceptions;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Infrastructure.Model;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexiCraft.Infrastructure.Middleware;

/// <summary>
/// 异常中间件
/// </summary>
public class ExceptionMiddleware(ILogger<ExceptionMiddleware> logger,
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
        catch (UserFriednlyException ex)
        {
            context!.Response.StatusCode = 499;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message, 499);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            logger.LogError($"Middleware UserFriednlyException Error:{ex.Message}");
        }
        catch (AntiforgeryValidationException ex)
        {
            context!.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            logger.LogError($"Middleware AntiforgeryValidationException Error:{ex.Message}");
        }
        catch (Exception ex)
        {
            context!.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            logger.LogError($"Middleware Exception Error:{ex.Message}");
        }
    }
}