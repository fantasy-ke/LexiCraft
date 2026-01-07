using BuildingBlocks.Extensions;
using BuildingBlocks.MongoDB.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Practice.AnswerEvaluation.Data.Repositories;
using LexiCraft.Services.Practice.MistakeAnalysis.Data.Repositories;
using LexiCraft.Services.Practice.PracticeTasks.Data.Repositories;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    
    public static IHostApplicationBuilder AddStorage(this IHostApplicationBuilder builder)
    {
        AddPracticeStorage(builder);
        AddRepositoryStorage(builder);

        return builder;
    }
    
    public static IHostApplicationBuilder AddPracticeStorage(IHostApplicationBuilder builder)
    {
        builder.AddResilience();
        
        builder.AddMongoDbContext<PracticeDbContext>();
        builder.AddMongoRepository<PracticeDbContext>();
        
        builder.Services.AddConfigurationOptions<ContextOption>();

        return builder;
    }
    
    
    private static void AddRepositoryStorage(IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IPracticeTaskRepository, PracticeTaskRepository>();
        builder.Services.AddTransient<IAnswerRecordRepository, AnswerRecordRepository>();
        builder.Services.AddTransient<IMistakeItemRepository, MistakeItemRepository>();
    }
}