using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain;
using BuildingBlocks.Extensions;
using BuildingBlocks.Filters;
using BuildingBlocks.Redis;
using BuildingBlocks.Shared;
using Lazy.Captcha.Core;
using LexiCraf.AuthServer.Application.Contract.Authorize;
using LexiCraf.AuthServer.Application.Contract.Authorize.Dto;
using LexiCraf.AuthServer.Application.Contract.Authorize.Input;
using LexiCraf.AuthServer.Application.Contract.Events;
using LexiCraf.AuthServer.Application.Contract.Exceptions;
using LexiCraft.AuthServer.Domain;
using LexiCraft.AuthServer.Domain.Users;
using LexiCraft.AuthServer.Domain.Users.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Z.EventBus;
using ZAnalyzers.Core;
using ZAnalyzers.Core.Attribute;

namespace LexiCraft.AuthServer.Application.Authorize;

[Route("/api/authorize")]
[Tags("Auth")]
[Filter(typeof(ResultEndPointFilter))]
public partial class AuthorizeService(
    IRepository<User> userRepository,
    IRepository<UserOAuth> userOAuthRepository,
    IHttpContextAccessor httpContextAccessor,
    IEventBus<LoginEto> loginEventBus,
    ICaptcha captcha,IJwtTokenProvider jwtTokenProvider,
    ICacheManager redisManager,ILogger<IAuthorizeService> logger,
    IHttpClientFactory httpClientFactory,
    IOptionsSnapshot<OAuthOption> oauthOption,IUserContext userContext):FantasyApi, IAuthorizeService
{
    [EndpointSummary("用户注册")]
    public async Task<bool> RegisterAsync(CreateUserRequest request)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (request.Email.IsNullEmpty() || !MyRegexEmail().IsMatch(request.Email))
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("邮箱格式不正确","", "Register")
                                        )));
        }

        if (request.Password.IsNullEmpty() 
            || request.Password.Length < 6 ||
            !MyRegexPd().IsMatch(request.Password)){
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("密码长度至少6位，且必须包含字母和数字","", "Register")
                            )));
        }

        if (request.CaptchaKey.IsNullEmpty() || request.CaptchaCode.IsNullEmpty())
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("请输入验证码","", "Register")
                            )));
        }
        
        if (request.UserAccount.IsNullEmpty() || request.UserAccount.IsNullEmpty())
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("请输入账号和用户名","", "Register")
                            )));
        }
        
        if (!captcha.Validate(request.CaptchaKey, request.CaptchaCode))
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("验证码校验错误","", "Register")
                            )));
        }

        var any = await userRepository.AnyAsync(p=>p.UserAccount == request.UserAccount);
        if (any)
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("当前用户名已存在，请重新输入","", "Register"))
                            ));
        }

        try
        {
            var user = new User(request.UserAccount, request.Email);
            user.SetPassword(request.Password);
            user.Avatar = "🦜";
            user.Roles.Add(RoleConstant.User);
            user.UpdateLastLogin();
            user.UpdateSource(SourceEnum.Register);
            await userRepository.InsertAsync(user);
            await userRepository.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{e.Message}用户注册失败");
            throw;
        }
    }

    private LoginEto GetLoginLogDto(HttpContext context, ExceptionLoginDto? exDto)
    {
        return new LoginEto(null, exDto?.UserAccount, null, DateTime.Now,
            context.GetRequestProperty("Origin"), context.GetRequestIp(),
            context.GetRequestProperty("Users-Agent"), exDto?.LoginType, false, exDto?.Message);
    }
    
    [EndpointSummary("登录接口")]
    public async Task<TokenResponse> LoginAsync(LoginTokenInput input)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (input.PassWord.IsNullEmpty()
            || input.PassWord.Length < 6 ||
            !MyRegexPd().IsMatch(input.PassWord))
        {
            
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("密码长度至少6位，且必须包含字母和数字",input.UserAccount, "Password")
                )));
        }

        if (input.UserAccount.IsNullEmpty())
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("请输入账号",input.UserAccount, "Password")
            )));
        }

        // if (input.CaptchaCode.IsNullEmpty())
        // {
        //     ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
        //         GetLoginLogDto(httpContext!,new ExceptionLoginDto("请输入验证码",input.UserAccount, "Password")
        //     )));
        // }
        // if (!captcha.Validate(input.CaptchaKey, input.CaptchaCode))
        // {
        //     ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
        //         GetLoginLogDto(httpContext!,new ExceptionLoginDto("验证码错误!",input.UserAccount, "Password")
        //     )));
        // }

        var user = await userRepository.QueryNoTracking<User>()
            .FirstOrDefaultAsync(x => x.UserAccount == input.UserAccount);

        if(user is null)
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("用户不存在",input.UserAccount, "Password")
            )));
        }

        if (!user.VerifyPassword(input.PassWord))
        {
            ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,new ExceptionLoginDto("密码错误",input.UserAccount, "Password")
            )));
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

        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.ToString("N")), res, TimeSpan.FromDays(7).Seconds);
        
        return res;
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [EndpointSummary("退出登录")]
    public async Task LoginOutAsync()
    {
        var userAccount = userContext.UserAccount;
        
        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, userAccount);
        await redisManager.DelAsync(cacheKey);
    }

    [EndpointSummary("第三方授权登录")]
    public async Task<string> OAuthTokenAsync(string type, string code, string state, string? redirectUri = null)
    {
        var client = httpClientFactory.CreateClient(nameof(AuthorizeService));
        var httpContext = httpContextAccessor.HttpContext;
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
                ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,    new ExceptionLoginDto("Github授权失败","", "OAuth")
                )));
            }

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/user")
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
                ThrowAuthLoginException.ThrowException(loginEventBus,JsonSerializer.Serialize(
                GetLoginLogDto(httpContext!,    new ExceptionLoginDto("Gitee授权失败","", "OAuth")
                )));
            }


            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://gitee.com/api/v5/user?access_token=" + result.AccessToken);

            var responseMessage = await client.SendAsync(request);

            userDto = await responseMessage.Content.ReadFromJsonAsync<OAuthUserDto>();
        }

        // 获取是否存在当前渠道
        var oauth = await userOAuthRepository.FirstOrDefaultAsync(x =>
            x.Provider == type && x.ProviderUserId == userDto.Id.ToString());

        User user;

        user = await userRepository.FirstOrDefaultAsync(x => x.Id == oauth.UserId);


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
    

    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex MyRegexPd();
    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$")]
    private static partial Regex MyRegexEmail();
}