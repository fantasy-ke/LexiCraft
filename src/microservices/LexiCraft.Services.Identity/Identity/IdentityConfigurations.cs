using LexiCraft.Services.Identity.Identity.Features.Login;
using LexiCraft.Services.Identity.Identity.Features.Logout;
using LexiCraft.Services.Identity.Identity.Features.OAuthToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity;

internal static class IdentityConfigurations
{
    private const string Tag = "Identity";
    private const string IdentityPrefixUri = $"{ApplicationConfiguration.IdentityModulePrefixUri}";

    internal static WebApplicationBuilder AddIdentityModuleServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapIdentityModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var identityVersionGroup = endpoints.NewVersionedApi(Tag).WithTags(Tag);

        var identityGroupV1 = identityVersionGroup
            .MapGroup(IdentityPrefixUri)
            .HasApiVersion(1.0);

        var identityGroupV2 = identityVersionGroup
            .MapGroup(IdentityPrefixUri)
            .HasApiVersion(2.0);


        identityGroupV1.MapOAuthEndpoint();
        identityGroupV1.MapLogoutEndpoint();
        identityGroupV1.MapLoginEndpoint();

        return endpoints;
    }
}
