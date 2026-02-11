using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Internal.Commands;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Authorize;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Services.Identity.Users.Internal.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    IQueryRepository<UserOAuth> userOAuthRepository) : IRequestHandler<OAuthCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(OAuthCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("接收到OAuth登录请求，类型: {Type}, 代码: {Code}", request.Type, request.Code);

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
                var tokenResponse = await HandlePostLoginAsync(user, request.Type, cancellationToken);

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();

                logger.LogInformation("OAuth登录处理完成，用户: {UserAccount}, 来源: {Source}", user.UserAccount, user.Source);

                return tokenResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OAuth登录链路异常");
                await unitOfWork.RollbackTransactionAsync();
                throw ex.ThrowUserFriendly(ex.Message, 500);
            }
        });
    }

    private async Task<OAuthUserDto> GetOAuthUserInfoAsync(OAuthCommand request)
    {
        var client = httpClientFactory.CreateClient(nameof(OAuthCommand));
        var provider = oauthProviderFactory.GetProvider(request.Type);
        if (provider is null) ThrowUserFriendlyException.ThrowException($"不支持的OAuth提供者: {request.Type}");
        try
        {
            var userInfo = await provider.GetUserInfoAsync(request.Code, request.RedirectUri, client);
            logger.LogDebug("获取到OAuth用户信息，ID: {OAuthId}, 账号: {Name}", userInfo.Id, userInfo.Name);
            return userInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Type}授权失败", request.Type);
            throw ex.ThrowUserFriendly($"{request.Type}授权失败");
        }
    }

    private async Task<User> ProcessUserLoginAsync(string provider, OAuthUserDto userDto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("开始处理用户登录/绑定逻辑，Provider: {Provider}, OAuthID: {OAuthId}", provider, userDto.Id);

        // 1. 优先查找 OAuth 绑定信息
        var oauth = await userOAuthRepository.Query()
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == userDto.Id, cancellationToken);

        User? user = null;

        if (oauth != null)
            // 老用户：直接通过绑定获取 User
            user = await userRepository.Query()
                .Include(u => u.OAuths)
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(x => x.Id == oauth.UserId, cancellationToken);

        if (user == null)
        {
            // 2. 尝试通过邮箱或账号查找用户（老用户未绑定或新用户）
            user = await userRepository.Query()
                .Include(u => u.OAuths)
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(
                    x => x.UserAccount == userDto.Name || (userDto.Email != null && x.Email == userDto.Email),
                    cancellationToken);

            if (user == null)
            {
                // 3. 创建新用户
                var source = provider.ToLower() switch
                {
                    "github" => SourceEnum.GitHub,
                    "gitee" => SourceEnum.Gitee,
                    _ => SourceEnum.Register
                };

                logger.LogInformation("未找到匹配用户，准备创建新用户，账户: {Name}", userDto.Name);

                user = await mediator.Send(new CreateUserCommand(
                    userDto.Name ?? userDto.Nickname ?? userDto.Id!,
                    userDto.Email ?? $"{userDto.Id}@{provider}.com",
                    null,
                    source,
                    userDto.AvatarUrl
                ), cancellationToken);
            }

            // 4. 绑定 OAuth 信息，回归子命令调用
            logger.LogInformation("正在为用户绑定OAuth信息，UserId: {UserId}, Provider: {Provider}", user.Id, provider);
            await mediator.Send(new BindUserOAuthCommand(user.Id, provider, userDto.Id!, user), cancellationToken);
        }

        // 5. 统一更新登录状态
        user.UpdateLastLoginTime();

        return user;
    }

    private async Task<TokenResponse> HandlePostLoginAsync(User user, string provider,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GenerateTokenResponseCommand(user, provider, $"{provider}登录成功"),
            cancellationToken);
    }
}