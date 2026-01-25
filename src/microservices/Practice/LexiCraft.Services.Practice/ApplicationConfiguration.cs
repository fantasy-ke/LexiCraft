// 练习服务的应用配置

using BuildingBlocks.Extensions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Practice.Assessments;
using LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;
using LexiCraft.Services.Practice.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice;

/// <summary>
///     练习模块的应用配置类
/// </summary>
public static class ApplicationConfiguration
{
    public const string PracticeModulePrefixUri = "api/v{version:apiVersion}/practice";

    /// <summary>
    ///     添加练习服务的应用程序服务
    /// </summary>
    /// <param name="builder">主机应用程序构建器</param>
    /// <returns>更新后的主机应用程序构建器</returns>
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddPracticeStorage();
        builder.Services.AddMediator<PracticeMetadata>();
        builder.AddInfrastructure();
        builder.AddTasksModuleServices();
        builder.AddAssessmentsModuleServices();
        builder.Services.WithMapster();
        builder.Services.WithIdGen();
        return builder;
    }

    /// <summary>
    ///     使用练习服务的应用程序中间件
    /// </summary>
    /// <param name="app">Web应用程序实例</param>
    /// <returns>更新后的端点路由构建器</returns>
    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {
        app.UseInfrastructure();

        app.MapTasksModuleEndpoints();
        app.MapAssessmentsModuleEndpoints();

        return app;
    }
}