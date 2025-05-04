using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Z.OSSCore.EntityType;
using Z.OSSCore.Interface;
using Z.OSSCore.Interface.Service;
using Z.OSSCore.Providers;
using Z.OSSCore.Services;

namespace Z.OSSCore
{
    public static class OssServiceExtensions
    {
        /// <summary>
        /// 添加对象存储服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="key">配置节点名称</param>
        /// <param name="oSSOptions">自定义配置操作</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOSSService(this IServiceCollection services, string key = "App:SSOConfig", Action<OSSOptions> oSSOptions = null)
        {
            using ServiceProvider provider = services.BuildServiceProvider();
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(IConfiguration));
            }
            
            IConfigurationSection section = configuration.GetSection(key);
            OSSOptions options = section.Get<OSSOptions>() ?? new OSSOptions();

            oSSOptions?.Invoke(options);

            if (!options.Enable)
                return services;
                
            services.Configure<OSSOptions>(option => { 
                option.Provider = options.Provider;
                option.Enable = options.Enable;
                option.DefaultBucket = options.DefaultBucket;
                option.Endpoint = options.Endpoint;  //不需要包含协议
                option.AccessKey = options.AccessKey;
                option.SecretKey = options.SecretKey;
                option.IsEnableHttps = options.IsEnableHttps;
                option.IsEnableCache = options.IsEnableCache; 
            });
            
            // 注册基础服务
            RegisterBaseServices(services);
            
            // 根据Provider注册对应的OSS服务
            switch (options.Provider)
            {
                case OSSProvider.Aliyun:
                    services.TryAddSingleton(sp => 
                        sp.GetRequiredService<IOSSServiceFactory<OSSAliyun>>().Create());
                    break;
                case OSSProvider.Minio:
                    services.TryAddSingleton(sp => 
                        sp.GetRequiredService<IOSSServiceFactory<OSSMinio>>().Create());
                    break;
                case OSSProvider.QCloud:
                    services.TryAddSingleton(sp => 
                        sp.GetRequiredService<IOSSServiceFactory<OSSQCloud>>().Create());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options.Provider)+"is invalid value.");
            }

            return services;
        }

        /// <summary>
        /// 添加默认配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="option">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOSSService(this IServiceCollection services, Action<OSSOptions> option)
        {
            return services.AddOSSService(oSSOptions: option);
        }
        
        /// <summary>
        /// 注册基础服务
        /// </summary>
        /// <param name="services">服务集合</param>
        private static void RegisterBaseServices(IServiceCollection services)
        {
            // 对于IOSSServiceFactory只需要注册一次
            if (services.Any(p => p.ServiceType == typeof(IOSSServiceFactory<>))) return;
            {
                // 如果未注册ICacheProvider则默认注册MemoryCacheProvider
                if (services.All(p => p.ServiceType != typeof(ICacheProvider)))
                {
                    services.AddMemoryCache();
                    services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
                }
                services.AddSingleton(typeof(IOSSServiceFactory<>), typeof(OSSServiceFactory<>));
            }
        }
        
    }
}