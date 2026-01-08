// 评估模块配置
using BuildingBlocks.Filters;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Assessments.Data.Repositories;
using LexiCraft.Services.Practice.Assessments.Data.EntityConfigurations;
using LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Assessments;

/// <summary>
/// 评估模块配置类
/// </summary>
public static class AssessmentsConfigurations
{
    public const string Tag = "Assessments";
    private const string PracticePrefixUri = $"{ApplicationConfiguration.PracticeModulePrefixUri}";

    /// <summary>
    /// 添加评估模块服务
    /// </summary>
    /// <param name="builder">主机应用程序构建器</param>
    /// <returns>更新后的主机应用程序构建器</returns>
    public static IHostApplicationBuilder AddAssessmentsModuleServices(this IHostApplicationBuilder builder)
    {
        // 注册仓库
        builder.Services.AddScoped<IAnswerRecordRepository, AnswerRecordRepository>();
        builder.Services.AddScoped<IMistakeItemRepository, MistakeItemRepository>();

        // 配置MongoDB实体映射
        AnswerRecordConfiguration.Configure();
        MistakeItemConfiguration.Configure();

        return builder;
    }

    /// <summary>
    /// 映射评估模块端点
    /// </summary>
    /// <param name="app">Web应用程序实例</param>
    /// <returns>更新后的端点路由构建器</returns>
    public static IEndpointRouteBuilder MapAssessmentsModuleEndpoints(this WebApplication app)
    {
        var assessmentsVersionGroup = app
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var assessmentsGroupV1 = assessmentsVersionGroup
            .MapGroup(PracticePrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        assessmentsGroupV1.MapSubmitAnswerEndpoint();

        return app;
    }
}