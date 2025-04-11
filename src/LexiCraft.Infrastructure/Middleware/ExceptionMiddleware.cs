using System.Diagnostics;
using System.Text.Json;
using LexiCraft.Infrastructure.Exceptions;
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
/// <param name="loggerFactory"></param>
public class ExceptionMiddleware(ILoggerFactory loggerFactory, IOptions<JsonOptions> options) : IMiddleware
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("ExceptionMiddleware");

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await next(context);
            stopwatch.Stop();
            _logger.LogInformation($"{context.Request.Path}--duration: {stopwatch.ElapsedMilliseconds} 毫秒");
        }
        catch (UserFriednlyException ex)
        {
            context!.Response.StatusCode = 499;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message, 499);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            _logger.LogError($"Middleware UserFriednlyException Error:{ex.Message}");
        }
        catch (AntiforgeryValidationException ex)
        {
            context!.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            _logger.LogError($"Middleware AntiforgeryValidationException Error:{ex.Message}");
        }
        catch (Exception ex)
        {
            context!.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            _logger.LogError($"Middleware Exception Error:{ex.Message}");
        }
    }
}