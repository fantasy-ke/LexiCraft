using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Features.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : ICommand<TokenResponse>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("刷新令牌不能为空");
    }
}

public class RefreshTokenCommandHandler(
    ICacheService cacheService,
    IUserRepository userRepository,
    IJwtTokenProvider jwtTokenProvider) : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var key = string.Format(UserInfoConst.RedisRefreshTokenKey, command.RefreshToken);
        var userIdValue = await cacheService.GetAsync<string>(key, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(userIdValue)) ThrowUserFriendlyException.ThrowException("刷新令牌无效或已过期");

        if (!Guid.TryParse(userIdValue, out var userId)) ThrowUserFriendlyException.ThrowException("刷新令牌无效");

        var user = await userRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user == null) ThrowUserFriendlyException.ThrowException("用户不存在");

        await cacheService.RemoveAsync(key, cancellationToken: cancellationToken);

        var userDict = new Dictionary<string, string>();
        var userForClaims = JsonSerializer.Deserialize<User>(JsonSerializer.Serialize(user, JsonSerializerOptions.Web));
        if (userForClaims != null)
        {
            userForClaims.ClearPassword();
            userDict.Add(UserInfoConst.UserId, user.Id.ToString());
            userDict.Add(UserInfoConst.UserName, user.Username);
            userDict.Add(UserInfoConst.UserAccount, user.UserAccount);
            userDict.Add("UserInfo", JsonSerializer.Serialize(userForClaims, JsonSerializerOptions.Web));
        }

        var token = jwtTokenProvider.GenerateAccessToken(userDict, user.Id.Value, user.Roles.ToArray());
        var newRefreshToken = jwtTokenProvider.GenerateRefreshToken();
        var response = new TokenResponse(token, newRefreshToken);

        await cacheService.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.Value.ToString("N")), response,
            options => options.Expiry = TimeSpan.FromDays(7), cancellationToken);
        await cacheService.SetAsync(string.Format(UserInfoConst.RedisRefreshTokenKey, newRefreshToken),
            user.Id.Value.ToString("N"), options => options.Expiry = TimeSpan.FromDays(7), cancellationToken);

        return response;
    }
}