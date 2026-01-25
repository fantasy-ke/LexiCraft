// 任务模块配置

using BuildingBlocks.Filters;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Tasks.Data.Repositories;
using LexiCraft.Services.Practice.Tasks.Features.CompletePractice;
using LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Tasks;

/// <summary>
///     任务模块配置类
/// </summary>
public static class TasksConfigurations
{
    public const string Tag = "Tasks";
    private const string PracticePrefixUri = $"{ApplicationConfiguration.PracticeModulePrefixUri}";

    /// <summary>
    ///     添加任务模块服务
    /// </summary>
    /// <param name="builder">主机应用程序构建器</param>
    /// <returns>更新后的主机应用程序构建器</returns>
    public static IHostApplicationBuilder AddTasksModuleServices(this IHostApplicationBuilder builder)
    {
        // 注册仓库
        builder.Services.AddScoped<IPracticeTaskRepository, PracticeTaskRepository>();

        return builder;
    }

    /// <summary>
    ///     映射任务模块端点
    /// </summary>
    /// <param name="app">Web应用程序实例</param>
    /// <returns>更新后的端点路由构建器</returns>
    public static IEndpointRouteBuilder MapTasksModuleEndpoints(this WebApplication app)
    {
        var tasksVersionGroup = app
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var tasksGroupV1 = tasksVersionGroup
            .MapGroup(PracticePrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        tasksGroupV1.MapCreatePracticeTaskEndpoint();
        tasksGroupV1.MapCompletePracticeEndpoint();

        return app;
    }
}