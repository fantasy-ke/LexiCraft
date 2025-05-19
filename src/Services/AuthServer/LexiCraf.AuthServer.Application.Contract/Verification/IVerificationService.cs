using LexiCraf.AuthServer.Application.Contract.Verification.Dto;

namespace LexiCraf.AuthServer.Application.Contract.Verification;

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