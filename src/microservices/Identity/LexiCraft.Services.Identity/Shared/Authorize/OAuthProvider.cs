using BuildingBlocks.Extensions.System;
using LexiCraft.Services.Identity.Shared.Dtos;
using MrHuo.OAuth;
using Serilog;

namespace LexiCraft.Services.Identity.Shared.Authorize;

/// <summary>
///     MrHuo.OAuth 桥接提供者 (泛型实现)
/// </summary>
public class OAuthProvider<TAccessToken, TUserInfo>(
    OAuthLoginBase<TAccessToken, TUserInfo> oauth,
    string providerName,
    Func<TUserInfo, OAuthUserDto> mapper) : IOAuthProvider
    where TAccessToken : class, IAccessTokenModel, new()
    where TUserInfo : class, IUserInfoModel
{
    public string ProviderName => providerName;

    public string GetAuthorizationUrl(string state)
    {
        return oauth.GetAuthorizeUrl(state);
    }

    public async Task<OAuthUserDto> GetUserInfoAsync(string code, string? redirectUri, HttpClient httpClient)
    {
        // 1. 获取 AccessToken
        var accessTokenModel = await oauth.GetAccessTokenAsync(code);

        Log.Logger.Information("获取 AccessToken 成功：{AccessToken}", accessTokenModel.ToJson());
        // 2. 获取用户信息
        var userInfo = await oauth.GetUserInfoAsync(accessTokenModel);

        Log.Logger.Information("获取用户信息成功：{UserInfo}", userInfo.ToJson());

        // 3. 映射到统一的 DTO
        return mapper(userInfo);
    }
}