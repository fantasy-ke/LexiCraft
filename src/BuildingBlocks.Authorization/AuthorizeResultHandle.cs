using System.Net;
using System.Text.Json;
using BuildingBlocks.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication;

public class AuthorizeResultHandle(ILogger<IAuthorizationMiddlewareResultHandler> logger, IOptions<JsonOptions> options) : IAuthorizationMiddlewareResultHandler
{

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        //返回鉴权失败信息
        if (authorizeResult.Challenged)
        {
            context!.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var response = ResultDto.Fail("Authentication failed, token invalid", 401);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
        
            return;
        }
        
        //返回授权失败信息
        if (authorizeResult.Forbidden)
        {
            var reason = string.Join(",", authorizeResult.AuthorizationFailure?.FailureReasons.Select(p => p.Message) ?? []);
            logger.LogWarning($"Authorization failed  with reason: {reason}");
        
            context!.Response.StatusCode = 599;
            context.Response.ContentType = "application/json";
            var response =  ResultDto.Fail( reason, 599);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options.Value.SerializerOptions));
            return;
        }

        await next(context);
    }
}