namespace LexiCraft.Application.Contract.Authorize.Input;

public class CreateUserRequest
{
    /// <summary>
    /// 昵称
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 用户密码
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    public string VerificationCode { get; set; }

    /// <summary>
    /// Captcha key
    /// </summary>
    public string CaptchaKey { get; set; }
}