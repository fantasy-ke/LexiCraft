using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.OSS;

public class OssServiceFactory(IOptionsMonitor<OSSOptions> optionsMonitor, ICacheProvider provider)
    : IOSSServiceFactory
{
    private readonly ICacheProvider _cache = provider ?? throw new ArgumentNullException(nameof(IMemoryCache));
    private OSSOptions Options => optionsMonitor.CurrentValue ?? throw new ArgumentNullException();

    public IOSSService Create()
    {
        if (string.IsNullOrEmpty(Options.DefaultBucket)) Options.DefaultBucket = DefaultOptionName.Name;
        if (Options == null ||
            (Options.Provider == OSSProvider.Invalid
             && string.IsNullOrEmpty(Options.Endpoint)
             && string.IsNullOrEmpty(Options.SecretKey)
             && string.IsNullOrEmpty(Options.AccessKey)))
            throw new ArgumentException($"Cannot get option by name '{Options?.DefaultBucket}'.");
        if (Options.Provider == OSSProvider.Invalid)
            throw new ArgumentNullException(nameof(Options.Provider));
        if (string.IsNullOrEmpty(Options.SecretKey))
            throw new ArgumentNullException(nameof(Options.SecretKey), "SecretKey can not null.");
        if (string.IsNullOrEmpty(Options.AccessKey))
            throw new ArgumentNullException(nameof(Options.AccessKey), "AccessKey can not null.");
        if ((Options.Provider == OSSProvider.Minio
             || Options.Provider == OSSProvider.QCloud)
            && string.IsNullOrEmpty(Options.Region))
            throw new ArgumentNullException(nameof(Options.Region),
                "When your provider is Minio, region can not null.");

        switch (Options.Provider)
        {
            case OSSProvider.Aliyun:
                return new AliyunOssService(_cache, Options);
            case OSSProvider.Minio:
                return new MinioOssService(_cache, Options);
            case OSSProvider.QCloud:
                return new QCloudOssService(_cache, Options);
            default:
                throw new Exception("未知的提供程序类型");
        }
    }
}