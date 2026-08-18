using BuildingBlocks.Filters;
using Fantasy.Services.Identity.Permissions.Features.AddPermission;
using Fantasy.Services.Identity.Permissions.Features.GetUserPermissions;
using Fantasy.Services.Identity.Permissions.Features.RemovePermission;
using Fantasy.Services.Identity.Permissions.Features.UpdatePermissions;
using Fantasy.Services.Identity.Permissions.Features.ValidatePermissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Fantasy.Services.Identity.Permissions;

internal static class PermissionsConfigurations
{
    public const string Tag = "Permissions";
    private const string PermissionsPrefixUri = $"{ApplicationConfiguration.IdentityModulePrefixUri}";

    internal static WebApplicationBuilder AddPermissionsModuleServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapPermissionsModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var permissionsVersionGroup = endpoints
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var permissionsGroupV1 = permissionsVersionGroup
            .MapGroup(PermissionsPrefixUri)
            .HasApiVersion(1.0)
            .WithoutResultDto();

        permissionsGroupV1.MapValidatePermissionsEndpoint();
        permissionsGroupV1.MapGetUserPermissionsEndpoint();
        permissionsGroupV1.MapAddPermissionEndpoint();
        permissionsGroupV1.MapRemovePermissionEndpoint();
        permissionsGroupV1.MapUpdatePermissionsEndpoint();

        return endpoints;
    }
}