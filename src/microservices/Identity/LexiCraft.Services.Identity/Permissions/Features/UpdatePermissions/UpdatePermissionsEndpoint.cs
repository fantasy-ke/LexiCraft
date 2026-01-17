using Humanizer;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions.Features.UpdatePermissions;

public static class UpdatePermissionsEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("permissions", Handle)
            .RequireAuthorization(IdentityPermissions.Permissions.Update)
            .WithName(nameof(UpdatePermissions))
            .WithDisplayName(nameof(UpdatePermissions).Humanize())
            .WithSummary("批量更新用户权限".Humanize())
            .WithDescription(nameof(UpdatePermissions).Humanize());

        async Task<UpdatePermissionsResponse> Handle(
            [AsParameters] UpdatePermissionsRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            var result = await mediator.Send(command, cancellationToken);

            return new UpdatePermissionsResponse(result);
        }
    }
}

/// <summary>
/// 批量更新权限请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record UpdatePermissionsRequestParameters(
    IMediator Mediator,
    [FromBody] UpdatePermissionsCommand Command,
    CancellationToken CancellationToken
);

/// <summary>
/// 批量更新权限响应
/// </summary>
/// <param name="Success">是否成功</param>
internal record UpdatePermissionsResponse(bool Success);
