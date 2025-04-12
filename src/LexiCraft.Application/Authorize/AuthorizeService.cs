using LexiCraft.Application.Contract.Authorize;
using LexiCraft.Application.Contract.Authorize.Input;
using LexiCraft.Domain;
using LexiCraft.Domain.Repository;
using LexiCraft.Domain.Users;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Application.Authorize;

public class AuthorizeService(IRepository<User> userRepository): IAuthorizeService
{
    [EndpointSummary("用户注册")]
    public async Task<bool> RegisterAsync(CreateUserRequest request)
    {
        var user = await userRepository.Select<UserSetting>().FirstOrDefaultAsync();
        return await Task.FromResult(true);
    }

    [EndpointSummary("登录接口")]
    public async Task<string> LoginAsync(LoginTokenInput input)
    {
        return await Task.FromResult("token");
    }

    [EndpointSummary("第三方授权登录")]
    public async Task<string> OAuthTokenAsync(string type, string code, string state, string? redirectUri = null)
    {
        return await Task.FromResult("token");
    }
}