using BuildingBlocks.Authentication;
using BuildingBlocks.Cors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {

        builder.AddDefaultCors();
        
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.RegisterAuthorization();
        
        builder.AddCustomAuthentication();
        return builder;
    }
}
