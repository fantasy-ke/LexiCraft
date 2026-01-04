using Humanizer;
using LexiCraft.Shared.Permissions;
using Mapster;
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
            .RequireAuthorization(PermissionsPermissions.Delete)
            .WithName(nameof(RemovePermission))
            .WithDisplayName(nameof(RemovePermission).Humanize())
            .WithSummary("删除用户权限".Humanize())
            .WithDescription(nameof(RemovePermission).Humanize());

        async Task<RemovePermissionResponse> Handle(
            [AsParameters] RemovePermissionRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;

            var result = await mediator.Send(request.Adapt<RemovePermissionCommand>(), cancellationToken);

            return result.Adapt<RemovePermissionResponse>();
        }
    }
}

/// <summary>
/// 删除权限请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record RemovePermissionRequestParameters(
    IMediator Mediator,
    [FromBody] RemovePermissionRequest Request,
    CancellationToken CancellationToken
);

/// <summary>
/// 删除权限请求DTO
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Permissions">权限名称列表</param>
public record RemovePermissionRequest(
    Guid UserId,
    List<string> Permissions);

/// <summary>
/// 删除权限响应
/// </summary>
/// <param name="Success">是否成功</param>
internal record RemovePermissionResponse(bool Success);
