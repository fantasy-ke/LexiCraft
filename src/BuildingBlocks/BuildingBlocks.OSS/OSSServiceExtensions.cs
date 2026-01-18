using BuildingBlocks.Extensions;
using BuildingBlocks.OSS.EntityType;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Providers;
using BuildingBlocks.OSS.Services;
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

            builder.Services.TryAddSingleton<IOSSService>(sp =>
                sp.GetRequiredService<IOSSServiceFactory>().Create());

            switch (options.Provider)
            {
                case OSSProvider.Aliyun:
                    builder.Services.TryAddSingleton<Interface.Service.IAliyunOssService>(sp =>
                        (Interface.Service.IAliyunOssService)sp.GetRequiredService<IOSSService>());
                    break;
                case OSSProvider.Minio:
                    builder.Services.TryAddSingleton<Interface.Service.IMinioOssService>(sp =>
                        (Interface.Service.IMinioOssService)sp.GetRequiredService<IOSSService>());
                    break;
                case OSSProvider.QCloud:
                    builder.Services.TryAddSingleton<Interface.Service.IQCloudOSSService>(sp =>
                        (Interface.Service.IQCloudOSSService)sp.GetRequiredService<IOSSService>());
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
            // 如果未注册 ICacheProvider 则默认注册 MemoryCacheProvider
            if (services.All(p => p.ServiceType != typeof(ICacheProvider)))
            {
                services.AddMemoryCache();
                services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
            }
            if (services.All(p => p.ServiceType != typeof(IOSSServiceFactory)))
            {
                services.AddSingleton<IOSSServiceFactory, OssServiceFactory>();
            }
        }
    }
}
