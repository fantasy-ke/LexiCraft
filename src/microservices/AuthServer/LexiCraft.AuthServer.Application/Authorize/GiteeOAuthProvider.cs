using System.Net.Http.Json;
using BuildingBlocks.Shared;
using Gnarly.Data;
using LexiCraft.AuthServer.Application.Contract.Authorize;
using LexiCraft.AuthServer.Application.Contract.Authorize.Dto;
using Microsoft.Extensions.Options;

namespace LexiCraft.AuthServer.Application.Authorize;

/// <summary>
/// Gitee OAuth提供者
/// </summary>
[Registration(typeof(IOAuthProvider))]
public class GiteeOAuthProvider(IOptionsSnapshot<OAuthOption> oauthOption) : IOAuthProvider,IScopeDependency
{
    public string ProviderName => "gitee";

    public async Task<OAuthUserDto> GetUserInfoAsync(string code, string? redirectUri, HttpClient httpClient)
    {
        // 获取Gitee配置
        var clientId = oauthOption.Value.Gitee.ClientId;
        var clientSecret = oauthOption.Value.Gitee.ClientSecret;

        // 获取access token
        var response = await httpClient.PostAsync(
            $"https://gitee.com/oauth/token?grant_type=authorization_code&redirect_uri={redirectUri}&response_type=code&code={code}&client_id={clientId}&client_secret={clientSecret}",
            null);

        var result = await response.Content.ReadFromJsonAsync<OAuthTokenDto>();
        if (result?.AccessToken is null)
        {
            throw new InvalidOperationException("Gitee授权失败：无法获取access token");
        }

        // 获取用户信息
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://gitee.com/api/v5/user?access_token=" + result.AccessToken);

        var responseMessage = await httpClient.SendAsync(request);
        var userDto = await responseMessage.Content.ReadFromJsonAsync<OAuthUserDto>();

        if (userDto is null)
        {
            throw new InvalidOperationException("Gitee授权失败：无法获取用户信息");
        }

        return userDto;
    }
}
