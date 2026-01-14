using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Authorize;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthToken;

public record OAuthCommand(string Type, string Code, string? RedirectUri) : IRequest<string>;

public class OAuthCommandValidator : AbstractValidator<OAuthCommand>
{
    public OAuthCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("OAuth类型不能为空")
            .Must(BeValidOAuthType).WithMessage("不支持的OAuth提供者类型");
            
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("授权码不能为空");
    }
    
    private bool BeValidOAuthType(string type)
    {
        // 根据OAuthProviderFactory中支持的类型进行验证
        var validTypes = new[] { "github", "gitee" };
        return validTypes.Contains(type.ToLower());
    }
}

internal class OAuthCommandHandler(
    OAuthProviderFactory oauthProviderFactory,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IJwtTokenProvider jwtTokenProvider,
    ILogger<OAuthCommandHandler> logger,
    IRepository<User> userRepository,
    IRepository<UserOAuth> userOAuthRepository) : IRequestHandler<OAuthCommand, string>
{
    public async Task<string> Handle(OAuthCommand request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(OAuthCommand));
        // 获取对应的OAuth提供者
        var provider = oauthProviderFactory.GetProvider(request.Type);
        if (provider is null)
        {
            // TODO: 实现异常处理逻辑
            throw new InvalidOperationException($"不支持的OAuth提供者: {request.Type}");
        }

        Shared.Dtos.OAuthUserDto userDto;
        try
        {
            // 使用提供者获取用户信息
            userDto = await provider.GetUserInfoAsync(request.Code, request.RedirectUri, client);
        }
        catch (Exception ex)
        {
            // TODO: 实现异常处理逻辑
            logger.LogError(ex, "{Type}授权失败", request.Type);
            throw new InvalidOperationException($"{request.Type}授权失败: {ex.Message}", ex);
        }

        // 获取是否存在当前渠道
        var oauth = await userOAuthRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Provider == request.Type && x.ProviderUserId == userDto.Id, cancellationToken);

        if (oauth is null)
        {
            // TODO: 实现异常处理逻辑
            throw new InvalidOperationException("用户未绑定该OAuth账户");
        }

        var user = await userRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Id == oauth.UserId, cancellationToken);
        if (user is null)
        {
            // TODO: 实现异常处理逻辑
            throw new InvalidOperationException("用户不存在");
        }

        var userDit = new Dictionary<string, string>();

        userDit.Add(UserInfoConst.UserId, user.Id.ToString());
        userDit.Add(UserInfoConst.UserName, user.Username);
        userDit.Add(UserInfoConst.UserAccount, user.UserAccount);
        userDit.Add("UserInfo", JsonSerializer.Serialize(user, JsonSerializerOptions.Web));
        // 注意：不再将权限添加到JWT中

        var token = jwtTokenProvider.GenerateAccessToken(userDit, user.Id, user.Roles.ToArray());

        return token;
    }
}