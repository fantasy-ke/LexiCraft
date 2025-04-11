namespace LexiCraft.Application.Contract.Authorize.Input;

public class LoginTokenInput
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; }
    

    /// <summary>
    /// Captcha key
    /// </summary>
    public string CaptchaKey { get; set; }

    /// <summary>
    /// Captcha code
    /// </summary>
    public string CaptchaCode { get; set; }
}