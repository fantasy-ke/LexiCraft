using AgileConfig.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Extensions;

public static class AgileConfigExtensions
{
    /// <summary>
    ///     统一集成 AgileConfig 到 IHostBuilder
    /// </summary>
    public static IHostBuilder UseAgileConfig(this IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) => { config.AddAgileConfig(); });

        return builder;
    }

    /// <summary>
    ///     统一集成 AgileConfig 到 IConfigurationBuilder
    /// </summary>
    public static IConfigurationBuilder AddZAgileConfig(this IConfigurationBuilder builder, string? sectionName = null)
    {
        var config = builder.Build();
        var options = config.BindOptions<ConfigClientOptions>(sectionName ?? nameof(ConfigClientOptions));

        // 如果配置中不存在 AgileConfig 节点，或者缺少关键信息 Nodes，则跳过
        if (string.IsNullOrEmpty(options.Nodes)) return builder;

        // 使用 Action<ConfigClientOptions> 重载，并将配置项映射
        builder.AddAgileConfig(opt =>
        {
            opt.Nodes = options.Nodes;
            opt.AppId = options.AppId;
            opt.Secret = options.Secret;
            opt.ENV = options.ENV;
        });

        return builder;
    }
}