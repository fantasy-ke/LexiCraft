using BuildingBlocks.Authentication;
using BuildingBlocks.Cors;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using BuildingBlocks.Validation.Extensions;
using BuildingBlocks.Validation.Pipelines;
using LexiCraft.Services.Identity.Shared.Authorization;
using MediatR;
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

        builder.RegisterAuthorization();
        builder.AddCustomAuthentication();
        // 注册权限定义提供程序
        builder.Services.AddPermissionDefinitionProvider<IdentityDefinitionProvider>();

        builder.AddCustomVersioning();
        builder.AddAspnetOpenApi(["v1", "v2"]);

        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>));

        builder.Services.AddCustomValidators(typeof(IdentityMetadata).Assembly);

        return builder;
    }
}