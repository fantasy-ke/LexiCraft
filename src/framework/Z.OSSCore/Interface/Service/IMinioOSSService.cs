﻿using Z.OSSCore.EntityType;
using Z.OSSCore.Models.Policy;

namespace Z.OSSCore.Interface.Service
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
