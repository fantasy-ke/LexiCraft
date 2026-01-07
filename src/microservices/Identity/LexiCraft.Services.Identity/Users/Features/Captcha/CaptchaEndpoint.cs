using Humanizer;
using Lazy.Captcha.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Users.Features.Captcha;

public static class CaptchaEndpoint
{
    internal static RouteHandlerBuilder MapCaptchaEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("users/captcha", Handle)
            .WithName("GetCaptcha")
            .WithDisplayName("Get Captcha".Humanize())
            .WithSummary("获取验证码".Humanize())
            .WithDescription("获取验证码图片和Key".Humanize())
            .AllowAnonymous();

        Task<CaptchaResponse> Handle([AsParameters] CaptchaRequestParameters requestParameters)
        {
            var captcha = requestParameters.Captcha;
            var captchaKey = Guid.NewGuid().ToString("N");
            var code = captcha.Generate(captchaKey);
            
            return Task.FromResult(new CaptchaResponse(captchaKey, $"data:image/png;base64,{code.Base64}"));
        }
    }
}

/// <summary>
/// 验证码请求参数
/// </summary>
/// <param name="Captcha"></param>
internal record CaptchaRequestParameters(
    ICaptcha Captcha
);

/// <summary>
/// 验证码响应
/// </summary>
/// <param name="CaptchaKey">验证码Key</param>
/// <param name="CaptchaData">验证码图片数据(Base64)</param>
internal record CaptchaResponse(
    string CaptchaKey, 
    string CaptchaData
);
