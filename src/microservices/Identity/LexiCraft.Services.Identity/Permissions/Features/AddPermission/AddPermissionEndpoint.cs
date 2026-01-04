using Humanizer;
using LexiCraft.Shared.Permissions;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions.Features.AddPermission;

public static class AddPermissionEndpoint
{
    internal static RouteHandlerBuilder MapAddPermissionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("permissions", Handle)
            .RequireAuthorization(PermissionsPermissions.Create)
            .WithName(nameof(AddPermission))
            .WithDisplayName(nameof(AddPermission).Humanize())
            .WithSummary("为用户新增权限".Humanize())
            .WithDescription(nameof(AddPermission).Humanize());

        async Task<AddPermissionResponse> Handle(
            [AsParameters] AddPermissionRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;

            var result = await mediator.Send(request.Adapt<AddPermissionCommand>(), cancellationToken);

            return result.Adapt<AddPermissionResponse>();
        }
    }
}

/// <summary>
/// 新增权限请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record AddPermissionRequestParameters(
    IMediator Mediator,
    [FromBody] AddPermissionRequest Request,
    CancellationToken CancellationToken
);

/// <summary>
/// 新增权限请求DTO
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Permissions">权限名称列表</param>
public record AddPermissionRequest(
    Guid UserId,
    List<string> Permissions);

/// <summary>
/// 新增权限响应
/// </summary>
/// <param name="Success">是否成功</param>
internal record AddPermissionResponse(bool Success);
