using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity;
using LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;
using LexiCraft.Services.Identity.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LexiCraft.Services.Identity;

public static class ApplicationConfiguration
{
    public const string IdentityModulePrefixUri = "api/v{version:apiVersion}/identity";

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddInfrastructure();
        
        builder.AddStorage();
        
        builder.Services.AddMediator<IdentityMetadata>();
        
        builder.Services.AddCaptcha(builder.Configuration);

        builder.AddGrpcService<IFilesService>(builder.Configuration);
        
        builder.Services.WithMapster();

        builder.Services.WithIdGen();
       
        return builder;
    }

    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {

        app.UseInfrastructure();
        
        app.MapGet("/", (HttpContext context) => "Identity Service Apis.")
            .ExcludeFromDescription();

        app.MapIdentityModuleEndpoints();
        app.MapUsersModuleEndpoints();

        return app;
    }
}
