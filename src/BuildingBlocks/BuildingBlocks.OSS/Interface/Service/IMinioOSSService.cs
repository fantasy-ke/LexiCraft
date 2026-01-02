using BuildingBlocks.OSS.EntityType;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Policy;

namespace BuildingBlocks.OSS.Interface.Service
{
    public interface IMinioOssService : IOSSService<OSSMinio>
    {
        Task<bool> RemoveIncompleteUploadAsync(string bucketName, string objectName);

        Task<List<ItemUploadInfo>> ListIncompleteUploads(string bucketName);

        Task<PolicyInfo> GetPolicyAsync(string bucketName);

        Task<bool> SetPolicyAsync(string bucketName, List<StatementItem> statements);

        Task<bool> RemovePolicyAsync(string bucketName);

        Task<bool> PolicyExistsAsync(string bucketName, StatementItem statement);
    }
}
