using BuildingBlocks.Filters;
using LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;
using LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;
using LexiCraft.Services.Practice.AnswerEvaluation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.AnswerEvaluation;

public static class AnswerEvaluationConfigurations
{
    public const string Tag = "AnswerEvaluation";
    private const string AnswerEvaluationPrefixUri = $"{ApplicationConfiguration.PracticeModulePrefixUri}";

    public static IHostApplicationBuilder AddAnswerEvaluationServices(this IHostApplicationBuilder builder)
    {
        // Register answer evaluation services
        builder.Services.AddTransient<IAnswerEvaluator, AnswerEvaluator>();
        
        return builder;
    }

    public static IEndpointRouteBuilder MapAnswerEvaluationModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var answerEvaluationVersionGroup = endpoints
            .NewVersionedApi(Tag);

        var answerEvaluationGroupV1 = answerEvaluationVersionGroup
            .MapGroup(AnswerEvaluationPrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        answerEvaluationGroupV1.MapSubmitAnswerEndpoint();
        answerEvaluationGroupV1.MapGetPracticeHistoryEndpoint();

        return endpoints;
    }
}