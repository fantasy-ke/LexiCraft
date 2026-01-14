using LexiCraft.Services.Identity.Shared.Dtos;

namespace LexiCraft.Services.Identity.Shared.Authorize;

/// <summary>
/// OAuth提供者接口
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// 提供者名称
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 获取OAuth授权URL
    /// </summary>
    /// <param name="state">状态参数，用于防止CSRF攻击</param>
    /// <returns>授权URL</returns>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向URI</param>
    /// <param name="httpClient">HTTP客户端</param>
    /// <returns>OAuth用户信息</returns>
    Task<OAuthUserDto> GetUserInfoAsync(string code, string? redirectUri, HttpClient httpClient);
}