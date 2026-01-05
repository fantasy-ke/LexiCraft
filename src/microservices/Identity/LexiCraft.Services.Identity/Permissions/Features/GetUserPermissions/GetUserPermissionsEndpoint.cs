using Humanizer;
using LexiCraft.Shared.Permissions;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions.Features.GetUserPermissions;

public static class GetUserPermissionsEndpoint
{
    internal static RouteHandlerBuilder MapGetUserPermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("permissions/{userId:guid}", Handle)
            .RequireAuthorization(IdentityPermissions.Permissions.Query)
            .WithName(nameof(GetUserPermissions))
            .WithDisplayName(nameof(GetUserPermissions).Humanize())
            .WithSummary("查询用户权限列表".Humanize())
            .WithDescription(nameof(GetUserPermissions).Humanize());

        async Task<GetUserPermissionsResponse> Handle(
            [AsParameters] GetUserPermissionsRequestParameters requestParameters
        )
        {
            var (queryProcessor, userId, cancellationToken) = requestParameters;
            var result = await queryProcessor.Send(new GetUserPermissionsQuery(userId), cancellationToken);

            return result.Adapt<GetUserPermissionsResponse>();
        }
    }
}

/// <summary>
///   查询用户权限列表请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserId"></param>
/// <param name="CancellationToken"></param>
internal record GetUserPermissionsRequestParameters(
    IMediator Mediator,
    Guid UserId,
    CancellationToken CancellationToken
);

/// <summary>
/// 查询用户权限列表响应
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Permissions">权限列表</param>
internal record GetUserPermissionsResponse(
    Guid UserId,
    List<string> Permissions
);
