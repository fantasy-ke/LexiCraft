using BuildingBlocks.OSS.Models;

namespace BuildingBlocks.OSS.Interface.Service;

public interface IAliyunOssService : IOSSService
{
    Task<string> GetBucketLocationAsync(string bucketName);

    Task<bool> SetBucketCorsRequestAsync(string bucketName, List<BucketCorsRule> rules);

    Task<string> GetBucketEndpointAsync(string bucketName);
}