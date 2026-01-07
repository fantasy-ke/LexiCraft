using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Practice.AnswerEvaluation;
using LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;
using LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;
using LexiCraft.Services.Practice.MistakeAnalysis;
using LexiCraft.Services.Practice.PracticeTasks;
using LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;
using LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;
using LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;
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
        
        // Add mistake analysis services
        builder.AddMistakeAnalysisServices();
        
        return builder;
    }

    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {
        app.UseInfrastructure();

        // Create API version set for Practice service
        var versionSet = app.NewApiVersionSet("Practice")
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        // Map versioned CQRS endpoints
        app.MapGeneratePracticeTasksEndpoint()
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0));
            
        app.MapSubmitAnswerEndpoint()
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0));
            
        app.MapGetPracticeHistoryEndpoint()
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0));
        
        // Map performance monitoring endpoint (already versioned)
        app.MapGetPerformanceMetricsEndpoint();

        return app;
    }
}