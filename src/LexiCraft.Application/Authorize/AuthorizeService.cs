using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lazy.Captcha.Core;
using LexiCraft.Application.Contract.Authorize;
using LexiCraft.Application.Contract.Authorize.Dto;
using LexiCraft.Application.Contract.Authorize.Input;
using LexiCraft.Application.Contract.Verification.Dto;
using LexiCraft.Domain;
using LexiCraft.Domain.Repository;
using LexiCraft.Domain.Users;
using LexiCraft.Domain.Users.Enum;
using LexiCraft.Infrastructure.Authorization;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using LexiCraft.Infrastructure.Exceptions;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Infrastructure.Redis;
using LexiCraft.Infrastructure.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexiCraft.Application.Authorize;

public partial class AuthorizeService(IRepository<User> userRepository,
    IRepository<UserOAuth> userOAuthRepository,
    ICaptcha captcha,IJwtTokenProvider jwtTokenProvider,
    ICacheManager redisManager,ILogger<IAuthorizeService> logger,
    IHttpClientFactory httpClientFactory,
    IOptionsSnapshot<OAuthOption> oauthOption,IUserContext userContext): IAuthorizeService
{
    [EndpointSummary("用户注册")]
    public async Task<bool> RegisterAsync(CreateUserRequest request)
    {
        if (request.Email.IsNullEmpty() || !MyRegexEmail().IsMatch(request.Email))
            ThrowUserFriendlyException.ThrowException("邮箱格式不正确");

        if (request.Password.IsNullEmpty() 
            || request.Password.Length < 6 ||
           !MyRegexPd().IsMatch(request.Password))
            ThrowUserFriendlyException.ThrowException("密码长度至少6位，且必须包含字母和数字");

        if (request.CaptchaKey.IsNullEmpty() || request.CaptchaCode.IsNullEmpty())
            ThrowUserFriendlyException.ThrowException("请输入验证码");

        if (request.UserAccount.IsNullEmpty() || request.UserAccount.IsNullEmpty())
            ThrowUserFriendlyException.ThrowException("请输入账号和用户名");

        if (!captcha.Validate(request.CaptchaKey, request.CaptchaCode))
            ThrowUserFriendlyException.ThrowException("验证码校验错误");

        var any = await userRepository.AnyAsync(p=>p.UserAccount == request.UserAccount);
        if (any)
            ThrowUserFriendlyException.ThrowException("当前用户名已存在，请重新输入");

        try
        {
            var user = new User(request.UserAccount, request.Email);
            user.SetPassword("Aa123456.");
            user.Avatar = "🦜";
            user.Roles.Add(RoleConstant.User);
            user.UpdateLastLogin();
            user.UpdateSource(SourceEnum.Register);
            await userRepository.InsertAsync(user);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.LogError(e, $"{e.Message}用户注册失败");
            throw;
        }
    }

    [EndpointSummary("登录接口")]
    public async Task<TokenResponse> LoginAsync(LoginTokenInput input)
    {
        if (input.PassWord.IsNullEmpty()
            || input.PassWord.Length < 6 ||
            !MyRegexPd().IsMatch(input.PassWord))
        {
            ThrowUserFriendlyException.ThrowException("密码长度至少6位，且必须包含字母和数字");
        }

        if (input.UserAccount.IsNullEmpty())
        {
            ThrowUserFriendlyException.ThrowException("请输入账号");
        }

        if (input.CaptchaCode.IsNullEmpty())
        {
            ThrowUserFriendlyException.ThrowException("请输入验证码");
        }
        if (!captcha.Validate(input.CaptchaKey, input.CaptchaCode))
        {
            // 发布登录事件
            ThrowUserFriendlyException.ThrowException("验证码错误!");
        }

        var user = await userRepository.QueryNoTracking<User>().FirstOrDefaultAsync(x => x.UserAccount == input.UserAccount);

        if(user is null)
        {
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        if (!user.VerifyPassword(input.PassWord))
        {
            ThrowUserFriendlyException.ThrowException("密码错误");
        }

        var userDit = new Dictionary<string, string>();

        user.PasswordHash = null;
        user.PasswordSalt = null;
        userDit.Add(UserInfoConst.UserId, user.Id.ToString());
        userDit.Add(UserInfoConst.UserName, user.Username);
        userDit.Add(UserInfoConst.UserAccount, user.UserAccount);
        userDit.Add("UserInfo", JsonSerializer.Serialize(user, JsonSerializerOptions.Web));

        var token = jwtTokenProvider.GenerateAccessToken(userDit, user.Id, user.Roles.ToArray());
        var refreshToken = jwtTokenProvider.GenerateRefreshToken();
        var res = new TokenResponse()
        {
            Token = token,
            RefreshToken = refreshToken
        };

        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id), res, TimeSpan.FromDays(7).Seconds);
        
        return res;
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [EndpointSummary("退出登录")]
    public async Task LoginOutAsync()
    {
        var userId = userContext.UserId;
        
        await redisManager.DelAsync(string.Format(UserInfoConst.RedisTokenKey, userId));
    }

    [EndpointSummary("第三方授权登录")]
    public async Task<string> OAuthTokenAsync(string type, string code, string state, string? redirectUri = null)
    {
        var client = httpClientFactory.CreateClient(nameof(AuthorizeService));

        OAuthUserDto? userDto = null;
        var soureceType = SourceEnum.Register;
        // 这里需要处理第三方登录的逻辑
        if (type.Equals("github"))
        {
            soureceType = SourceEnum.GitHub;
            // 处理github登录
            var clientId = oauthOption.Value.GitHub.ClientId;
            var clientSecret = oauthOption.Value.GitHub.ClientSecret;

            var response =
                await client.PostAsync(
                    $"https://github.com/login/oauth/access_token?code={code}&client_id={clientId}&client_secret={clientSecret}",
                    null);

            var result = await response.Content.ReadFromJsonAsync<OAuthTokenDto>();
            if (result is null)
            {
                throw new Exception("Github授权失败");
            }

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/user")
            {
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken)
                }
            };

            var responseMessage = await client.SendAsync(request);

            userDto = await responseMessage.Content.ReadFromJsonAsync<OAuthUserDto>();
        }
        else if (type.Equals("gitee"))
        {
            soureceType = SourceEnum.Gitee;
            // 处理gitee登录
            var clientId = oauthOption.Value.Gitee.ClientId;
            var clientSecret = oauthOption.Value.Gitee.ClientSecret;

            var response =
                await client.PostAsync(
                    $"https://gitee.com/oauth/token?grant_type=authorization_code&redirect_uri={redirectUri}&response_type=code&code={code}&client_id={clientId}&client_secret={clientSecret}",
                    null);

            var result = await response.Content.ReadFromJsonAsync<OAuthTokenDto>();
            if (result?.AccessToken is null)
            {
                throw new Exception("Gitee授权失败");
            }


            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://gitee.com/api/v5/user?access_token=" + result.AccessToken);

            var responseMessage = await client.SendAsync(request);

            userDto = await responseMessage.Content.ReadFromJsonAsync<OAuthUserDto>();
        }

        // 获取是否存在当前渠道
        var oauth = await userOAuthRepository.FirstOrDefaultAsync(x =>
            x.Provider == type && x.ProviderUserId == userDto.Id.ToString());

        User user;

        if (oauth == null)
        {
            // 如果邮箱是空则随机生成
            if (string.IsNullOrEmpty(userDto.Email))
            {
                userDto.Email = "oauth_" + userDto.Id + "@token-ai.cn";
            }


            // 创建一个新的用户
            user = new User((userDto?.Name ?? userDto?.Id.ToString()), userDto.Email);

            user.SetPassword("Aa123456.");
            user.Avatar = userDto.AvatarUrl ?? "🦜";
            user.Roles.Add(RoleConstant.User);
            user.UpdateLastLogin();
            user.UpdateSource(soureceType);
            oauth = new UserOAuth()
            {
                AccessToken = string.Empty,
                Provider = type,
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ProviderUserId = userDto.Id.ToString(),
                RefreshToken = string.Empty,
                AccessTokenExpiresAt = DateTimeOffset.Now.AddDays(1),
            };
            user = await userRepository.InsertAsync(user);
            await userOAuthRepository.InsertAsync(oauth);
        }
        else
        {
            user = await userRepository.FirstOrDefaultAsync(x => x.Id == oauth.UserId);
        }


        var userDit = new Dictionary<string, string>();

        user.PasswordHash = null;
        user.PasswordSalt = null;
        userDit.Add(UserInfoConst.UserId, user.Id.ToString());
        userDit.Add(UserInfoConst.UserName, user.Username);
        userDit.Add(UserInfoConst.UserAccount, user.UserAccount);
        userDit.Add("UserInfo", JsonSerializer.Serialize(user, JsonSerializerOptions.Web));

        var token = jwtTokenProvider.GenerateAccessToken(userDit, user.Id, user.Roles.ToArray());

        return token;
    }

    [GeneratedRegex(@"^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex MyRegexPd();
    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$")]
    private static partial Regex MyRegexEmail();
}