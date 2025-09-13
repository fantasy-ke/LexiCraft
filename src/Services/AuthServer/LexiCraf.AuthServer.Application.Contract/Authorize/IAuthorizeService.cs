using LexiCraf.AuthServer.Application.Contract.Authorize.Dto;
using LexiCraf.AuthServer.Application.Contract.Authorize.Input;

namespace LexiCraf.AuthServer.Application.Contract.Authorize;

public interface IAuthorizeService
{
    /// <summary>
    /// 用户注册请求
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<bool> RegisterAsync(CreateUserRequest request);
    
    /// <summary>
    /// token授权码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<TokenResponse> LoginAsync(LoginTokenInput input);
    
    
    /// <summary>
    /// 退出登录
    /// </summary>
    /// <returns></returns>
    Task LoginOutAsync();

    /// <summary>
    /// 第三方平台登录
    /// </summary>
    /// <param name="type"></param>
    /// <param name="code"></param>
    /// <param name="state"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    Task<string> OAuthTokenAsync(string type, string code, string state, string? redirectUri = null);
}
