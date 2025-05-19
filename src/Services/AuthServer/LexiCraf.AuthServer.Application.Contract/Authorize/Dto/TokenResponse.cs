namespace LexiCraf.AuthServer.Application.Contract.Authorize.Dto;

/// <summary>
/// 登录返回token 响应
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// 刷新token
    /// </summary>
    public string RefreshToken { get; set; }
}