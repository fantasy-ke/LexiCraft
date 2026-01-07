using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Practice.AnswerEvaluation;
using LexiCraft.Services.Practice.MistakeAnalysis;
using LexiCraft.Services.Practice.PracticeTasks;
using LexiCraft.Services.Practice.Shared;
using LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Practice;

public static class ApplicationConfiguration
{
    public const string PracticeModulePrefixUri = "api/v{version:apiVersion}/practice";

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddStorage();
        builder.Services.AddMediator<PracticeMetadata>();
        builder.AddInfrastructure();
        builder.AddEventPublishing();
        builder.AddGrpcService<IFilesService>(builder.Configuration);
        builder.Services.WithMapster();
        builder.Services.WithIdGen();
        
        // Add exception handling
        builder.AddPracticeExceptionHandling();
        
        // Add database resilience and performance monitoring
        builder.AddDatabaseResilience();
        // Add practice task services
        builder.AddPracticeTasksServices();
        // Add answer evaluation services
        builder.AddAnswerEvaluationServices();
        
        
        return builder;
    }

    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {
        app.UseInfrastructure();

        // Map module endpoints using the new Vocabulary-style approach
        app.MapPracticeTasksModuleEndpoints();
        app.MapAnswerEvaluationModuleEndpoints();
        app.MapSharedModuleEndpoints();

        return app;
    }
}