using LexiCraft.Services.Practice.MistakeAnalysis.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.MistakeAnalysis;

public static class MistakeAnalysisConfigurations
{
    public static IHostApplicationBuilder AddMistakeAnalysisServices(this IHostApplicationBuilder builder)
    {
        // Register mistake analysis services
        builder.Services.AddTransient<IErrorClassifier, ErrorClassifier>();
        
        return builder;
    }
}