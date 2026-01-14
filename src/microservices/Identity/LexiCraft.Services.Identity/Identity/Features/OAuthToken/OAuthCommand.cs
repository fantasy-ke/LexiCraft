using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Features.GenerateToken;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Authorize;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Services.Identity.Users.Features.BindUserOAuth;
using LexiCraft.Services.Identity.Users.Features.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthToken;

public record OAuthCommand(string Type, string Code, string? RedirectUri) : IRequest<TokenResponse>;

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
    IUnitOfWork unitOfWork,
    ILogger<OAuthCommandHandler> logger,
    IMediator mediator,
    IRepository<User> userRepository,
    IRepository<UserOAuth> userOAuthRepository) : IRequestHandler<OAuthCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(OAuthCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. 获取OAuth用户信息
                var userDto = await GetOAuthUserInfoAsync(request);

                // 2. 处理登录与绑定逻辑
                var user = await ProcessUserLoginAsync(request.Type, userDto, cancellationToken);

                // 3. 生成Token与处理后续逻辑
                return await HandlePostLoginAsync(user, request.Type, cancellationToken);
            }
            catch (Exception)
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });
    }

    private async Task<OAuthUserDto> GetOAuthUserInfoAsync(OAuthCommand request)
    {
        var client = httpClientFactory.CreateClient(nameof(OAuthCommand));
        var provider = oauthProviderFactory.GetProvider(request.Type);
        if (provider is null)
        {
            ThrowUserFriendlyException.ThrowException($"不支持的OAuth提供者: {request.Type}");
        }
        try
        {
            return await provider.GetUserInfoAsync(request.Code, request.RedirectUri, client);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Type}授权失败", request.Type);
            throw ex.ThrowUserFriendly($"{request.Type}授权失败");
        }
    }

    private async Task<User> ProcessUserLoginAsync(string provider, Shared.Dtos.OAuthUserDto userDto,
        CancellationToken cancellationToken)
    {
        // 查找OAuth绑定
        var oauth = await userOAuthRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == userDto.Id, cancellationToken);

        User? user;

        if (oauth != null)
        {
            user = await userRepository.QueryNoTracking()
                .FirstOrDefaultAsync(x => x.Id == oauth.UserId, cancellationToken);
        }
        else
        {
            // 绑定不存在，尝试通过账号理论或邮箱查找用户
            user = await userRepository.QueryNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserAccount == userDto.Name || (userDto.Email != null && x.Email == userDto.Email),
                    cancellationToken);

            if (user == null)
            {
                // 用户不存在，创建新用户
                var source = provider.ToLower() switch
                {
                    "github" => SourceEnum.GitHub,
                    "gitee" => SourceEnum.Gitee,
                    _ => SourceEnum.Register
                };

                user = await mediator.Send(new CreateUserCommand(
                    userDto.Name ?? userDto.Nickname ?? userDto.Id!,
                    userDto.Email ?? $"{userDto.Id}@{provider}.com",
                    null,
                    source,
                    userDto.AvatarUrl
                ), cancellationToken);
            }

            // 绑定OAuth信息
            await mediator.Send(new BindUserOAuthCommand(user.Id, provider, userDto.Id!), cancellationToken);
        }

        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        return user;
    }

    private async Task<TokenResponse> HandlePostLoginAsync(User user, string provider,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GenerateTokenResponseCommand(user, provider, $"{provider}登录成功"),
            cancellationToken);
    }
}