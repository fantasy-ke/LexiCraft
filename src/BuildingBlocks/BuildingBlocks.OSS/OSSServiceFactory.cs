using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Services;

namespace BuildingBlocks.OSS
{
    public class OssServiceFactory<T>(IOptionsMonitor<OSSOptions> optionsMonitor, ICacheProvider provider)
        : IOSSServiceFactory<T>
    {
        private OSSOptions Options => optionsMonitor.CurrentValue ?? throw new ArgumentNullException();
        private readonly ICacheProvider _cache = provider ?? throw new ArgumentNullException(nameof(IMemoryCache));

        public IOSSService<T> Create()
        {
            #region 参数验证

            if (string.IsNullOrEmpty(Options.DefaultBucket))
            {
                Options.DefaultBucket = DefaultOptionName.Name;
            }
            if (Options == null ||
                Options.Provider == OSSProvider.Invalid
                && string.IsNullOrEmpty(Options.Endpoint)
                && string.IsNullOrEmpty(Options.SecretKey)
                && string.IsNullOrEmpty(Options.AccessKey))
                throw new ArgumentException($"Cannot get option by name '{ Options?.DefaultBucket}'.");
            if (Options.Provider == OSSProvider.Invalid)
                throw new ArgumentNullException(nameof(Options.Provider));
            if (string.IsNullOrEmpty(Options.SecretKey))
                throw new ArgumentNullException(nameof(Options.SecretKey), "SecretKey can not null.");
            if (string.IsNullOrEmpty(Options.AccessKey))
                throw new ArgumentNullException(nameof(Options.AccessKey), "AccessKey can not null.");
            if ((Options.Provider == OSSProvider.Minio
                || Options.Provider == OSSProvider.QCloud)
                && string.IsNullOrEmpty(Options.Region))
            {
                throw new ArgumentNullException(nameof(Options.Region), "When your provider is Minio, region can not null.");
            }

            #endregion

                // 如果没有找到匹配的类型，回退到根据Provider选择
                switch (Options.Provider)
                {
                    case OSSProvider.Aliyun:
                        return (IOSSService<T>)new AliyunOssService(_cache, Options);
                    case OSSProvider.Minio:
                        return (IOSSService<T>)new MinioOssService(_cache, Options);
                    case OSSProvider.QCloud:
                        return (IOSSService<T>)new QCloudOssService(_cache, Options);
                    default:
                        throw new Exception("未知的提供程序类型");
                }
            
        }
    }
}
