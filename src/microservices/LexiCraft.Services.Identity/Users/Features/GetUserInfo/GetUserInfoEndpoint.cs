using BuildingBlocks.Authentication.Contract;
using Humanizer;
using LexiCraft.Services.Identity.Users.Dtos;
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
            .MapGet("/api/users", Handle)
            .RequireAuthorization(policyNames: [])
            .WithTags("Users")
            .WithName(nameof(GetUserInfo))
            .WithDisplayName(nameof(GetUserInfo).Humanize())
            .WithSummary("获取当前登录用户信息".Humanize())
            .WithDescription(nameof(GetUserInfo).Humanize());

        async Task<GetUserInfoResponse> Handle(
            [AsParameters] GetUserInfoRequestParameters requestParameters
        )
        {
            var (queryProcessor,userContext, cancellationToken) = requestParameters;
            var result = await queryProcessor.Send(new GetUserInfoQuery(userContext.UserId), cancellationToken);
            return new GetUserInfoResponse(result);
        }
    }
}

/// <summary>
///   获取当前登录用户信息
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserContext"></param>
/// <param name="CancellationToken"></param>
internal record GetUserInfoRequestParameters(
    IMediator Mediator, 
    IUserContext UserContext,
    CancellationToken CancellationToken
);

internal record GetUserInfoResponse(UserInfoDto? UserInfo);
