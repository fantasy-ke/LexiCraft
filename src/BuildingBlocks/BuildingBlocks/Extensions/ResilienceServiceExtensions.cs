using BuildingBlocks.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Extensions;

/// <summary>
/// 通用弹性服务注册扩展
/// </summary>
public static class ResilienceServiceExtensions
{
    /// <summary>
    /// 添加通用弹性服务配置
    /// </summary>
    /// <param name="builder">应用构建器</param>
    /// <param name="sectionName">配置节名称，默认为 "Resilience"</param>
    /// <returns></returns>
    public static IHostApplicationBuilder AddResilience(
        this IHostApplicationBuilder builder, 
        string? sectionName = null)
    {
        // 注册弹性配置选项
        builder.Services.AddConfigurationOptions<ResilienceOptions>(
            sectionName ?? nameof(ResilienceOptions));
        
        return builder;
    }
}