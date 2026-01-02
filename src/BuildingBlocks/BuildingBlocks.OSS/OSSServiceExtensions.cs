using BuildingBlocks.Extensions;
using BuildingBlocks.OSS.EntityType;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.OSS
{
    public static class OssServiceExtensions
    {
        /// <summary>
        /// 添加对象存储服务 (支持多种提供商与配置文件绑定)
        /// </summary>
        public static IHostApplicationBuilder AddOssService(this IHostApplicationBuilder builder, Action<OSSOptions>? configure = null)
        {
            // 使用 BuildingBlocks 扩展方法绑定配置
            var options = builder.Configuration.BindOptions(nameof(OSSOptions), configure);

            // 注册 Options 以供后续注入
            builder.Services.AddConfigurationOptions(nameof(OSSOptions), configure);

            if (!options.Enable)
                return builder;

            // 注册基础服务
            RegisterBaseServices(builder.Services);

            // 根据 Provider 注册对应的 OSS 服务
            switch (options.Provider)
            {
                case OSSProvider.Aliyun:
                    builder.Services.TryAddSingleton(sp =>
                        sp.GetRequiredService<IOSSServiceFactory<OSSAliyun>>().Create());
                    break;
                case OSSProvider.Minio:
                    builder.Services.TryAddSingleton(sp =>
                        sp.GetRequiredService<IOSSServiceFactory<OSSMinio>>().Create());
                    break;
                case OSSProvider.QCloud:
                    builder.Services.TryAddSingleton(sp =>
                        sp.GetRequiredService<IOSSServiceFactory<OSSQCloud>>().Create());
                    break;
            }

            return builder;
        }

        /// <summary>
        /// 注册基础服务
        /// </summary>
        /// <param name="services">服务集合</param>
        private static void RegisterBaseServices(IServiceCollection services)
        {
            // 对于 IOSSServiceFactory 只需要注册一次
            if (services.Any(p => p.ServiceType == typeof(IOSSServiceFactory<>))) return;
            
            // 如果未注册 ICacheProvider 则默认注册 MemoryCacheProvider
            if (services.All(p => p.ServiceType != typeof(ICacheProvider)))
            {
                services.AddMemoryCache();
                services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
            }
            services.AddSingleton(typeof(IOSSServiceFactory<>), typeof(OssServiceFactory<>));
        }
    }
}