using BuildingBlocks.Authentication.Contract;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Users.Features.GetUserInfo;

public static class GetUserInfoEndpoint
{
    internal static RouteHandlerBuilder MapGetUserInfoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("users/info", Handle)
            .RequireAuthorization()
            .WithName(nameof(GetUserInfo))
            .WithDisplayName(nameof(GetUserInfo).Humanize())
            .WithSummary("获取当前用户信息".Humanize())
            .WithDescription(nameof(GetUserInfo).Humanize());

        async Task<GetUserInfoResponse> Handle(
            [AsParameters] GetUserInfoRequestParameters requestParameters
        )
        {
            var (queryProcessor, userContext, cancellationToken) = requestParameters;
            var result = await queryProcessor.Send(new GetUserInfoQuery(userContext.UserId), cancellationToken);

            return new GetUserInfoResponse(
                result.UserId,
                result.UserName,
                result.Email,
                result.Phone,
                result.Avatar
            );
        }
    }
}

/// <summary>
///     获取当前登录用户信息
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserContext"></param>
/// <param name="CancellationToken"></param>
internal record GetUserInfoRequestParameters(
    IMediator Mediator,
    IUserContext UserContext,
    CancellationToken CancellationToken
);

/// <summary>
///     获取用户信息响应
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="UserName">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="Phone">手机号</param>
/// <param name="Avatar">头像</param>
internal record GetUserInfoResponse(
    Guid UserId,
    string UserName,
    string Email,
    string? Phone,
    string Avatar
);