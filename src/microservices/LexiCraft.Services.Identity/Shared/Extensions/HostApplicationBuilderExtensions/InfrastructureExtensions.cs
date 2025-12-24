using Asp.Versioning;
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

        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;

            options.DefaultApiVersion = new ApiVersion(1, 0);
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";

            options.SubstituteApiVersionInUrl = true;
        })
        .EnableApiVersionBinding();
        return builder;
    }
}
