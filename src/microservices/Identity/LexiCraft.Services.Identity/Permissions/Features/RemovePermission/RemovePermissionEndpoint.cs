using Humanizer;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions.Features.RemovePermission;

public static class RemovePermissionEndpoint
{
    internal static RouteHandlerBuilder MapRemovePermissionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("permissions", Handle)
            .RequireAuthorization(IdentityPermissions.Permissions.Delete)
            .WithName(nameof(RemovePermission))
            .WithDisplayName(nameof(RemovePermission).Humanize())
            .WithSummary("删除用户权限".Humanize())
            .WithDescription(nameof(RemovePermission).Humanize());

        async Task<RemovePermissionResponse> Handle(
            [AsParameters] RemovePermissionRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            var result = await mediator.Send(command, cancellationToken);

            return new RemovePermissionResponse(result);
        }
    }
}

/// <summary>
/// 删除权限请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
internal record RemovePermissionRequestParameters(
    IMediator Mediator,
    [FromBody] RemovePermissionCommand Command,
    CancellationToken CancellationToken
);

/// <summary>
/// 删除权限响应
/// </summary>
/// <param name="Success">是否成功</param>
internal record RemovePermissionResponse(bool Success);
