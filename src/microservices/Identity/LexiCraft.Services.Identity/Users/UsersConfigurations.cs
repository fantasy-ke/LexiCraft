using BuildingBlocks.Filters;
using LexiCraft.Services.Identity.Users.Features.Captcha;
using LexiCraft.Services.Identity.Users.Features.GetUserInfo;
using LexiCraft.Services.Identity.Users.Features.RegisterUser;
using LexiCraft.Services.Identity.Users.Features.UpdateUserInfo;
using LexiCraft.Services.Identity.Users.Features.UploadAvatar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Users;

internal static class UsersConfigurations
{
    public const string Tag = "Users";
    private const string IdentityPrefixUri = $"{ApplicationConfiguration.IdentityModulePrefixUri}";

    internal static WebApplicationBuilder AddUsersModuleServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapUsersModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var usersVersionGroup = endpoints
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var usersGroupV1 = usersVersionGroup
            .MapGroup(IdentityPrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();
        ;


        usersGroupV1.MapRegisterEndpoint();
        usersGroupV1.MapCaptchaEndpoint();
        usersGroupV1.MapGetUserInfoEndpoint();
        usersGroupV1.MapUpdateUserInfoEndpoint();
        usersGroupV1.MapUploadAvatarEndpoint();

        return endpoints;
    }
}