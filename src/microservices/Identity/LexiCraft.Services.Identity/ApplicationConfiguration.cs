using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity;
using LexiCraft.Services.Identity.Permissions;
using LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;
using LexiCraft.Services.Identity.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity;

public static class ApplicationConfiguration
{
    public const string IdentityModulePrefixUri = "api/v{version:apiVersion}/identity";

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddStorage();
        builder.Services.AddMediator<IdentityMetadata>();
        builder.AddInfrastructure();
        builder.AddGrpcService<IFilesService>(builder.Configuration);
        builder.Services.WithMapster();
        builder.Services.WithIdGen();
        builder.AddIdentityModuleServices();
        return builder;
    }

    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {

        app.UseInfrastructure();

        app.MapIdentityModuleEndpoints();
        app.MapUsersModuleEndpoints();
        app.MapPermissionsModuleEndpoints();

        return app;
    }
}
