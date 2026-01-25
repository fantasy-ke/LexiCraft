using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Dto;
using BuildingBlocks.OSS.Utils;

namespace BuildingBlocks.OSS.Interface.Base;

public abstract class BaseOSSService(ICacheProvider cache, OSSOptions options)
{
    private readonly ICacheProvider _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    public OSSOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    public virtual Task RemovePresignedUrlCache(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        if (Options.IsEnableCache)
        {
            var key = BuildPresignedObjectCacheKey(input.BucketName, input.ObjectName, PresignedObjectType.Get);
            _cache.Remove(key);
            key = BuildPresignedObjectCacheKey(input.BucketName, input.ObjectName, PresignedObjectType.Put);
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    internal virtual string FormatObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || objectName == "/") throw new ArgumentNullException(nameof(objectName));
        if (objectName.StartsWith('/')) return objectName.TrimStart('/');
        return objectName;
    }

    internal virtual async Task<string> PresignedObjectAsync(string bucketName
        , string objectName
        , int expiresInt
        , PresignedObjectType type
        , Func<string, string, int, Task<string>> PresignedFunc)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            objectName = FormatObjectName(objectName);
            if (expiresInt <= 0) throw new Exception("ExpiresIn time can not less than 0.");
            if (expiresInt > 7 * 24 * 3600) throw new Exception("ExpiresIn time no more than 7 days.");
            const int minExpiresInt = 600;
            if (Options.IsEnableCache && expiresInt > minExpiresInt)
            {
                var key = BuildPresignedObjectCacheKey(bucketName, objectName, type);
                var cacheResult = _cache.Get<PresignedUrlCache>(key);
                var cache = cacheResult;
                //Unix时间
                var nowTime = TimeUtil.Timestamp();
                //缓存中存在，且有效时间不低于10分钟
                if (cache != null
                    && cache.Type == type
                    && cache.CreateTime > 0
                    && cache.CreateTime + expiresInt - nowTime > minExpiresInt
                    && cache.Name == objectName
                    && cache.BucketName == bucketName)
                    return cache.Url;

                var presignedUrl = await PresignedFunc(bucketName, objectName, expiresInt);
                if (string.IsNullOrEmpty(presignedUrl)) throw new Exception("Presigned object url failed.");
                var urlCache = new PresignedUrlCache
                {
                    Url = presignedUrl,
                    CreateTime = nowTime,
                    Name = objectName,
                    BucketName = bucketName,
                    Type = type
                };
                var randomSec = new Random().Next(0, 10);
                _cache.Set(key, urlCache, TimeSpan.FromSeconds(expiresInt + randomSec));
                return urlCache.Url;
            }
            else
            {
                var presignedUrl = await PresignedFunc(bucketName, objectName, expiresInt);
                if (string.IsNullOrEmpty(presignedUrl)) throw new Exception("Presigned object url failed.");
                return presignedUrl;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Presigned {(type == PresignedObjectType.Get ? "get" : "put")} url for object '{objectName}' from {bucketName} failed. {ex.Message}",
                ex);
        }
    }

    private string BuildPresignedObjectCacheKey(string bucketName, string objectName, PresignedObjectType type)
    {
        return "OSS:" + Encrypt.MD5($"{GetType().FullName}_{bucketName}_{objectName}_{type.ToString().ToUpper()}");
    }
}