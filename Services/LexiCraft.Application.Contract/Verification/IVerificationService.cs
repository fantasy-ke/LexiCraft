using LexiCraft.Application.Contract.Verification.Dto;

namespace LexiCraft.Application.Contract.Verification;

public interface IVerificationService
{
    /// <summary>
    /// 获取验证码
    /// </summary>
    /// <param name="key">
    /// 验证码类型 
    /// </param>
    /// <returns></returns>
    Task<VerificationDto> GetCaptchaCodeAsync(string key);
}