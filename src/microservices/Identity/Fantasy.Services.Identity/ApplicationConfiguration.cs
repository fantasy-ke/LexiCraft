using BuildingBlocks.Extensions;
using BuildingBlocks.Filters;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using Fantasy.Services.Identity.Identity;
using Fantasy.Services.Identity.Permissions;
using Fantasy.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
using Fantasy.Services.Identity.Shared.Extensions.WebApplicationExtensions;
using Fantasy.Services.Identity.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Fantasy.Services.Identity;

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

        var api = app.MapGroup(string.Empty).WithResultDto();
        api.MapIdentityModuleEndpoints();
        api.MapUsersModuleEndpoints();
        api.MapPermissionsModuleEndpoints();

        return app;
    }
}