using BuildingBlocks.Authentication;
using LexiCraft.Services.Identity.Permissions.Features.AddPermission;
using LexiCraft.Services.Identity.Permissions.Features.GetUserPermissions;
using LexiCraft.Services.Identity.Permissions.Features.RemovePermission;
using LexiCraft.Services.Identity.Permissions.Features.UpdatePermissions;
using LexiCraft.Services.Identity.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Permissions;

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
            .HasApiVersion(1.0);

        permissionsGroupV1.MapGetUserPermissionsEndpoint();
        permissionsGroupV1.MapAddPermissionEndpoint();
        permissionsGroupV1.MapRemovePermissionEndpoint();
        permissionsGroupV1.MapUpdatePermissionsEndpoint();

        return endpoints;
    }
}
