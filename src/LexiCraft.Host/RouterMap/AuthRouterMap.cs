using LexiCraft.Application.Contract.Authorize;
using LexiCraft.Application.Contract.Authorize.Input;
using LexiCraft.Infrastructure.Filters;

namespace LexiCraft.Host.RouterMap;

public static class AuthRouterMap
{
    public static WebApplication MapAuthEndpoint(this WebApplication app)
    {
        var aAuthorize = app
            .MapGroup("/api/v1/Authorize")
            .WithDescription("授权鉴权")
            .WithTags("auth")
            .AddEndpointFilter<ResultEndPointFilter>();

        aAuthorize.MapPost("/Login",
            async (IAuthorizeService authorize, LoginTokenInput input) => await authorize.LoginAsync(input))
            .WithSummary("登录接口"); 

        aAuthorize.MapPost("/OAuthToken",
            async (IAuthorizeService authorize, string type, string code, string state, string? redirectUri) => await authorize.OAuthTokenAsync(type, code, state, redirectUri))
            .WithSummary("第三方授权登录"); 

        aAuthorize.MapPost("/Register",
            async (IAuthorizeService authorize, CreateUserRequest input) => await authorize.RegisterAsync(input))
            .WithSummary("用户注册"); 


        return app;
    }
}