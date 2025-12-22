using System.Net.Http.Json;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.Extensions.Options;

namespace LexiCraft.Services.Identity.Shared.Authorize;

/// <summary>
/// GitHub OAuth提供者
/// </summary>
public class GitHubOAuthProvider(IOptionsSnapshot<OAuthOption> oauthOption) : IOAuthProvider
{
    public string ProviderName => "github";

    public async Task<OAuthUserDto> GetUserInfoAsync(string code, string? redirectUri, HttpClient httpClient)
    {
        // 获取GitHub配置
        var clientId = oauthOption.Value.GitHub.ClientId;
        var clientSecret = oauthOption.Value.GitHub.ClientSecret;

        // 获取access token
        var response = await httpClient.PostAsync(
            $"https://github.com/login/oauth/access_token?code={code}&client_id={clientId}&client_secret={clientSecret}",
            null);

        var result = await response.Content.ReadFromJsonAsync<OAuthTokenDto>();
        if (result is null)
        {
            throw new InvalidOperationException("GitHub授权失败：无法获取access token");
        }

        // 获取用户信息
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);

        var responseMessage = await httpClient.SendAsync(request);
        var userDto = await responseMessage.Content.ReadFromJsonAsync<OAuthUserDto>();

        if (userDto is null)
        {
            throw new InvalidOperationException("GitHub授权失败：无法获取用户信息");
        }

        return userDto;
    }
}