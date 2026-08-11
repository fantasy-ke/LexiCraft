using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Dto;
using BuildingBlocks.OSS.Models.Policy;

namespace BuildingBlocks.OSS.Services;

internal sealed class DisabledOSSService : IOSSService
{
    public Task<bool> BucketExistsAsync(string bucketName) => Disabled<bool>();

    public Task<bool> CreateBucketAsync(string bucketName) => Disabled<bool>();

    public Task<bool> RemoveBucketAsync(string bucketName) => Disabled<bool>();

    public Task<List<Bucket>> ListBucketsAsync() => Disabled<List<Bucket>>();

    public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode) => Disabled<bool>();

    public Task<AccessMode> GetBucketAclAsync(string bucketName) => Disabled<AccessMode>();

    public Task<bool> ObjectsExistsAsync(string bucketName, string objectName) => Disabled<bool>();

    public Task<List<Item>> ListObjectsAsync(string bucketName, string? prefix = null) => Disabled<List<Item>>();

    public Task GetObjectAsync(
        string bucketName,
        string objectName,
        Action<Stream> callback,
        CancellationToken cancellationToken = default) => Disabled();

    public Task GetObjectAsync(
        string bucketName,
        string objectName,
        string fileName,
        CancellationToken cancellationToken = default) => Disabled();

    public Task<ObjectOutPut> GetObjectAsync(GetObjectInput input) => Disabled<ObjectOutPut>();

    public Task<bool> UploadObjectAsync(UploadObjectInput input) => Disabled<bool>();

    public Task<bool> PutObjectAsync(
        string bucketName,
        string objectName,
        Stream data,
        CancellationToken cancellationToken = default) => Disabled<bool>();

    public Task<bool> PutObjectAsync(
        string bucketName,
        string objectName,
        string filePath,
        CancellationToken cancellationToken = default) => Disabled<bool>();

    public Task<ItemMeta> GetObjectMetadataAsync(
        string bucketName,
        string objectName,
        string? versionId = null,
        string? matchEtag = null,
        DateTime? modifiedSince = null) => Disabled<ItemMeta>();

    public Task<bool> CopyObjectAsync(
        string bucketName,
        string objectName,
        string? destBucketName,
        string? destObjectName = null) => Disabled<bool>();

    public Task<bool> RemoveObjectAsync(OperateObjectInput input) => Disabled<bool>();

    public Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNames) => Disabled<bool>();

    public Task RemovePresignedUrlCache(OperateObjectInput input) => Disabled();

    public Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt) =>
        Disabled<string>();

    public Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt) =>
        Disabled<string>();

    public Task<bool> SetObjectAclAsync(string bucketName, string objectName, AccessMode mode) => Disabled<bool>();

    public Task<AccessMode> GetObjectAclAsync(OperateObjectInput input) => Disabled<AccessMode>();

    public Task<AccessMode> RemoveObjectAclAsync(OperateObjectInput input) => Disabled<AccessMode>();

    private static Task Disabled()
    {
        return Task.FromException(CreateException());
    }

    private static Task<T> Disabled<T>()
    {
        return Task.FromException<T>(CreateException());
    }

    private static InvalidOperationException CreateException()
    {
        return new InvalidOperationException(
            "Object storage is disabled. Set OSSOptions:Enable to true and configure a provider before using IOSSService.");
    }
}
