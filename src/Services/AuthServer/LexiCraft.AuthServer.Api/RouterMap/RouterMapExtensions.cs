using BuildingBlocks.Filters;
using LexiCraf.AuthServer.Application.Contract.Authorize;
using LexiCraf.AuthServer.Application.Contract.Authorize.Input;
using LexiCraf.AuthServer.Application.Contract.Verification;

namespace LexiCraft.AuthServer.Api.RouterMap;

public static class RouterMapExtensions
{
    public static WebApplication MapAuthEndpoint(this WebApplication app)
    {
        #region auth

        var aAuthorize = app
            .MapGroup("/api/v1/Authorize")
            .WithDescription("授权登入登出")
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

        aAuthorize.MapGet("/LoginOut",
                async (IAuthorizeService authorize) => await authorize.LoginOutAsync())
            .WithSummary("退出登录");

        #endregion

        #region verification

        var verification = app
            .MapGroup("/api/v1/Verification")
            .WithDescription("验证码")
            .WithTags("verification")
            .AddEndpointFilter<ResultEndPointFilter>();

        verification.MapGet("/GetCaptchaCode",
                async (IVerificationService verificationService, string key) => await verificationService.GetCaptchaCodeAsync(key))
            .WithSummary("获取验证码");

        #endregion

        return app;
    }
}