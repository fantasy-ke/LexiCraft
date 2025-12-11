using LexiCraft.AuthServer.Application.Contract.Authorize.Dto;

namespace LexiCraft.AuthServer.Application.Contract.Authorize;

/// <summary>
/// OAuth提供者接口
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// 提供者名称（如：github, gitee）
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// 获取OAuth用户信息
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向URI</param>
    /// <param name="httpClient">HTTP客户端</param>
    /// <returns>OAuth用户信息</returns>
    Task<OAuthUserDto> GetUserInfoAsync(string code, string? redirectUri, HttpClient httpClient);
}
