using BuildingBlocks.Authentication;
using BuildingBlocks.Cors;
using BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;
using BuildingBlocks.Validation.Extensions;
using BuildingBlocks.Validation.Pipelines;
using LexiCraft.Services.Vocabulary.Shared.Authorization;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Vocabulary.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultCors();
        
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.RegisterAuthorization();
        builder.AddCustomAuthentication();
        builder.Services.AddPermissionDefinitionProvider<VocabularyPermissionDefinitionProvider>();
        
        builder.AddCustomVersioning();
        builder.AddAspnetOpenApi(["v1"]);
        
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>));

        builder.Services.AddCustomValidators(typeof(VocabularyMetadata).Assembly);
        
        return builder;
    }
}
