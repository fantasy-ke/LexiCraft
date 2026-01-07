using LexiCraft.Services.Practice.PracticeTasks.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.PracticeTasks;

public static class PracticeTasksConfigurations
{
    public static IHostApplicationBuilder AddPracticeTasksServices(this IHostApplicationBuilder builder)
    {
        // Register practice task services
        builder.Services.AddTransient<IPracticeTaskGenerator, PracticeTaskGenerator>();
        builder.Services.AddTransient<IPracticeTaskService, PracticeTaskService>();
        
        // Register vocabulary service client
        builder.Services.AddTransient<IVocabularyServiceClient, VocabularyServiceClient>();
        
        // Register HTTP client for vocabulary service
        builder.Services.AddHttpClient<VocabularyServiceClient>(client =>
        {
            // This will be configured via service discovery in production
            // For now, use a placeholder base address
            client.BaseAddress = new Uri("http://vocabulary-service/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}