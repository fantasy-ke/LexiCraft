﻿using Lazy.Captcha.Core;
using LexiCraft.Application.Contract.Verification;
using LexiCraft.Application.Contract.Verification.Dto;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Application.Verification;

public class VerificationService(ICaptcha captcha): IVerificationService
{
    /// <summary>
    /// 获取验证码, 返回验证码图片base64
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [EndpointSummary("获取验证码")]
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