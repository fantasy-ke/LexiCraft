using Asp.Versioning;
using BuildingBlocks.Authentication;
using BuildingBlocks.Cors;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {

        builder.AddDefaultCors();
        
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.RegisterAuthorization();
        
        builder.AddCustomAuthentication();
        
        builder.AddCustomVersioning();
        builder.AddAspnetOpenApi(["v1", "v2"]);
        
        return builder;
    }
}
