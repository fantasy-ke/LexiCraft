using BuildingBlocks.Authentication;
using BuildingBlocks.Filters;
using Lazy.Captcha.Core;
using LexiCraft.AuthServer.Application.Contract.Verification;
using LexiCraft.AuthServer.Application.Contract.Verification.Dto;
using Microsoft.AspNetCore.Http;
using ZAnalyzers.Core;

namespace LexiCraft.AuthServer.Application.Verification;

[Route("/api/Verification")]
[Tags("verification")]
[Filter(typeof(ResultEndPointFilter))]
// [ZAuthorize("Verification.Page")]
public class VerificationService(ICaptcha captcha):FantasyApi, IVerificationService
{
    /// <summary>
    /// 获取验证码, 返回验证码图片base64
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [EndpointSummary("获取验证码")]
    [ZAuthorize("Verification.Add","Verification.Edit")]
    public Task<VerificationDto> GetCaptchaCodeAsync(string key)
    {
        var uuid = key + ":" + Guid.NewGuid().ToString("N");

        var code = captcha.Generate(uuid, 240);

        return Task.FromResult(new VerificationDto
        {
            Key = uuid,
            Code = "data:image/png;base64," + code.Base64
        });
    }
}