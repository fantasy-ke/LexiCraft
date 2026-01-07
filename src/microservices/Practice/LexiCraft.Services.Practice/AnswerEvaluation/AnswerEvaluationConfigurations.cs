using LexiCraft.Services.Practice.AnswerEvaluation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.AnswerEvaluation;

public static class AnswerEvaluationConfigurations
{
    public static IHostApplicationBuilder AddAnswerEvaluationServices(this IHostApplicationBuilder builder)
    {
        // Register answer evaluation services
        builder.Services.AddTransient<IAnswerEvaluator, AnswerEvaluator>();
        
        return builder;
    }
}