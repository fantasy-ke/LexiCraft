using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Z.OSSCore.EntityType;
using Z.OSSCore.Interface;
using Z.OSSCore.Interface.Service;
using Z.OSSCore.Services;

namespace Z.OSSCore
{
    public class OSSServiceFactory<T>(IOptions<OSSOptions> optionsMonitor, ICacheProvider provider)
        : IOSSServiceFactory<T>
    {
        private readonly OSSOptions _options = optionsMonitor.Value ?? throw new ArgumentNullException();
        private readonly ICacheProvider _cache = provider ?? throw new ArgumentNullException(nameof(IMemoryCache));

        public IOSSService<T> Create()
        {
            #region 参数验证

            if (string.IsNullOrEmpty(_options.DefaultBucket))
            {
                _options.DefaultBucket = DefaultOptionName.Name;
            }
            if (_options == null ||
                _options.Provider == OSSProvider.Invalid
                && string.IsNullOrEmpty(_options.Endpoint)
                && string.IsNullOrEmpty(_options.SecretKey)
                && string.IsNullOrEmpty(_options.AccessKey))
                throw new ArgumentException($"Cannot get option by name '{ _options.DefaultBucket}'.");
            if (_options.Provider == OSSProvider.Invalid)
                throw new ArgumentNullException(nameof(_options.Provider));
            if (string.IsNullOrEmpty(_options.SecretKey))
                throw new ArgumentNullException(nameof(_options.SecretKey), "SecretKey can not null.");
            if (string.IsNullOrEmpty(_options.AccessKey))
                throw new ArgumentNullException(nameof(_options.AccessKey), "AccessKey can not null.");
            if ((_options.Provider == OSSProvider.Minio
                || _options.Provider == OSSProvider.QCloud)
                && string.IsNullOrEmpty(_options.Region))
            {
                throw new ArgumentNullException(nameof(_options.Region), "When your provider is Minio, region can not null.");
            }

            #endregion

                // 如果没有找到匹配的类型，回退到根据Provider选择
                switch (_options.Provider)
                {
                    case OSSProvider.Aliyun:
                        return (IOSSService<T>)new AliyunOssService(_cache, _options);
                    case OSSProvider.Minio:
                        return (IOSSService<T>)new MinioOssService(_cache, _options);
                    case OSSProvider.QCloud:
                        return (IOSSService<T>)new QCloudOssService(_cache, _options);
                    default:
                        throw new Exception("未知的提供程序类型");
                }
            
        }
    }
}