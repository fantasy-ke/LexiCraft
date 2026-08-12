using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions.Features.ValidatePermissions;

public static class ValidatePermissionsEndpoint
{
    internal static RouteHandlerBuilder MapValidatePermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("permissions/validate", Handle)
            .RequireAuthorization()
            .WithName(nameof(ValidatePermissions))
            .WithDisplayName(nameof(ValidatePermissions).Humanize())
            .WithSummary("验证当前用户权限")
            .WithDescription("供 LexiCraft 业务服务验证当前 Bearer Token 会话和权限");
    }

    private static Task<PermissionValidationResult> Handle(
        PermissionValidationRequest request,
        IPermissionCheck permissionCheck,
        CancellationToken cancellationToken)
    {
        return permissionCheck.CheckAsync(request.Permissions ?? [], cancellationToken);
    }
}