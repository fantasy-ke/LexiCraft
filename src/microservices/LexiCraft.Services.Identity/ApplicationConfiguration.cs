using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity;
using LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;
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
        builder.AddStorage();
        
        builder.Services.AddMediator<IdentityMetadata>();
        
        builder.Services.AddCaptcha(builder.Configuration);

        builder.AddGrpcService<IFilesService>(builder.Configuration);

        
        builder.Services.WithMapster();

        builder.Services.WithIdGen();
        return builder;
    }

    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (HttpContext context) => "Identity Service Apis.")
            .ExcludeFromDescription();

        endpoints.MapIdentityModuleEndpoints();

        return endpoints;
    }
}
