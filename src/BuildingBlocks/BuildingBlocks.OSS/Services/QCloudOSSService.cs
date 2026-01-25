using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Interface.Base;
using BuildingBlocks.OSS.Interface.Service;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Dto;
using BuildingBlocks.OSS.Models.Exceptions;
using BuildingBlocks.OSS.Models.Policy;
using BuildingBlocks.OSS.Providers;
using COSXML;
using COSXML.Auth;
using COSXML.Common;
using COSXML.CosException;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using COSXML.Model.Service;
using COSXML.Model.Tag;

namespace BuildingBlocks.OSS.Services;

public class QCloudOssService : BaseOSSService, IQCloudOSSService
{
    public QCloudOssService(ICacheProvider cache, OSSOptions options)
        : base(cache, options)
    {
        var config = new CosXmlConfig.Builder()
            .IsHttps(options.IsEnableHttps)
            .SetRegion(options.Region)
            .SetDebugLog(false)
            .Build();
        QCloudCredentialProvider cosCredentialProvider =
            new DefaultQCloudCredentialProvider(options.AccessKey, options.SecretKey, 600);
        Context = new CosXmlServer(config, cosCredentialProvider);
    }

    public CosXml Context { get; }

    #region private

    private string ConvertBucketName(string input)
    {
        return $"{input}-{Options.Endpoint}";
    }

    #endregion

    #region bucekt

    public Task<bool> BucketExistsAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        var request = new HeadBucketRequest(bucketName);
        try
        {
            var result = Context.HeadBucket(request);
            return Task.FromResult(true);
        }
        catch (CosClientException ex)
        {
            throw new Exception($"Rquest client error, {ex.Message}", ex);
        }
        catch (CosServerException ex)
        {
            if (ex.statusCode == 403) return Task.FromResult(true);

            if (ex.statusCode == 404) return Task.FromResult(false);

            throw new Exception($"Server error, {ex.Message}", ex);
        }
    }

    public Task<bool> CreateBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        try
        {
            var request = new PutBucketRequest(bucketName);
            //执行请求
            Context.PutBucket(request);
            return Task.FromResult(true);
        }
        catch (CosServerException serverEx)
        {
            if (serverEx.statusCode == 409 &&
                serverEx.statusMessage.Equals("Conflict", StringComparison.OrdinalIgnoreCase))
                throw new BucketExistException($"Bucket '{bucketName}' already exists.", serverEx);
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Create bucket {bucketName} failed, {ex.Message}", ex);
        }
    }

    public Task<List<Bucket>> ListBucketsAsync()
    {
        var request = new GetServiceRequest();
        var result = Context.GetService(request);
        if (result == null || result.listAllMyBuckets == null) throw new Exception("List buckets result is null.");
        //得到所有的 buckets
        List<ListAllMyBuckets.Bucket> allBuckets = result.listAllMyBuckets.buckets;
        List<Bucket> buckets = [];
        foreach (var item in allBuckets)
            buckets.Add(new Bucket
            {
                Location = item.location,
                Name = item.name,
                Owner = new Owner
                {
                    Id = result.listAllMyBuckets.owner.id,
                    Name = result.listAllMyBuckets.owner.disPlayName
                },
                CreationDate = item.createDate
            });
        return Task.FromResult(buckets);
    }

    public Task<bool> RemoveBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        var request = new DeleteBucketRequest(bucketName);
        //执行请求
        var result = Context.DeleteBucket(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        var acl = mode switch
        {
            AccessMode.Default => CosACL.Private,
            AccessMode.Private => CosACL.Private,
            AccessMode.PublicRead => CosACL.PublicRead,
            AccessMode.PublicReadWrite => CosACL.PublicReadWrite,
            _ => CosACL.Private
        };
        var request = new PutBucketACLRequest(bucketName);
        //设置私有读写权限
        request.SetCosACL(acl);
        //执行请求
        var result = Context.PutBucketACL(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<AccessMode> GetBucketAclAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        var request = new GetBucketACLRequest(bucketName);
        //执行请求
        var result = Context.GetBucketACL(request);
        //存储桶的 ACL 信息
        var acl = result.accessControlPolicy;

        var isPublicRead = false;
        var isPublicWrite = false;
        if (acl is { accessControlList.grants.Count: > 0 })
            foreach (var item in acl.accessControlList.grants)
            {
                if (string.IsNullOrEmpty(item.grantee.uri)) continue;
                if (item.grantee.uri.Contains("allusers", StringComparison.OrdinalIgnoreCase))
                    switch (item.permission.ToLower())
                    {
                        case "read":
                            isPublicRead = true;
                            break;
                        case "write":
                            isPublicWrite = true;
                            break;
                    }
            }

        //结果
        if (isPublicRead && !isPublicWrite) return Task.FromResult(AccessMode.PublicRead);

        if (isPublicRead && isPublicWrite) return Task.FromResult(AccessMode.PublicReadWrite);

        if (!isPublicRead && isPublicWrite) return Task.FromResult(AccessMode.Private);

        return Task.FromResult(AccessMode.Private);
    }

    #endregion

    #region Object

    public Task<bool> ObjectsExistsAsync(string bucketName, string objectName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        bucketName = ConvertBucketName(bucketName);
        try
        {
            //build request
            var request = new HeadObjectRequest(bucketName, objectName);
            //执行请求
            var result = Context.HeadObject(request);
            if (result.IsSuccessful()) return Task.FromResult(true);

            return Task.FromResult(false);
        }
        catch (CosServerException ex)
        {
            if (ex.statusCode == 404) return Task.FromResult(false);
            throw;
        }
    }

    public Task<List<Item>> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        bucketName = ConvertBucketName(bucketName);
        ListBucket? info = null;
        string? nextMarker = null;
        List<Item> items = [];
        do
        {
            var request = new GetBucketRequest(bucketName);
            if (!string.IsNullOrEmpty(nextMarker)) request.SetMarker(nextMarker);
            if (!string.IsNullOrEmpty(prefix)) request.SetPrefix(prefix);
            //执行请求
            var result = Context.GetBucket(request);
            //bucket的相关信息
            info = result.listBucket;
            if (info.isTruncated)
                // 数据被截断，记录下数据下标
                nextMarker = info.nextMarker;
            foreach (var item in info.contentsList)
                items.Add(new Item
                {
                    Key = item.key,
                    LastModified = item.lastModified,
                    ETag = item.eTag,
                    Size = (ulong)item.size,
                    IsDir = !string.IsNullOrWhiteSpace(item.key) && item.key[^1] == '/',
                    BucketName = bucketName,
                    VersionId = null
                });
        } while (info.isTruncated);

        return Task.FromResult(items);
    }

    public async Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (!await ObjectsExistsAsync(bucketName, objectName))
            throw new Exception($"Object '{objectName}' not in bucket '{bucketName}'");
        bucketName = ConvertBucketName(bucketName);

        await Task.Run(() =>
        {
            var request = new GetObjectBytesRequest(bucketName, objectName);
            //执行请求
            var result = Context.GetObject(request);
            //获取内容
            var content = result.content;
            if (content is { Length: > 0 })
            {
                var ms = new MemoryStream(content);
                callback(ms);
            }
            else
            {
                throw new Exception("Get object bytes is null.");
            }
        }, cancellationToken);
    }

    public async Task GetObjectAsync(string bucketName, string objectName, string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
        if (!await ObjectsExistsAsync(bucketName, objectName))
            throw new Exception($"Object '{objectName}' not in bucket '{bucketName}'");
        bucketName = ConvertBucketName(bucketName);
        var fullPath = Path.GetFullPath(fileName);
        var parentPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath)) Directory.CreateDirectory(parentPath);
        await Task.Run(() =>
        {
            var request = new GetObjectRequest(bucketName, objectName, parentPath, Path.GetFileName(fullPath));
            Context.GetObject(request);
        }, cancellationToken);
    }

    public async Task<ObjectOutPut> GetObjectAsync(GetObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        if (!await ObjectsExistsAsync(input.BucketName, input.ObjectName))
            throw new Exception($"Object '{input.ObjectName}' not in bucket '{input.BucketName}'");
        input.BucketName = ConvertBucketName(input.BucketName);

        var request = new GetObjectBytesRequest(input.BucketName, input.BucketName);
        //执行请求
        var result = Context.GetObject(request);
        var content = result.content;
        MemoryStream? memoryStream = null;
        if (content is { Length: > 0 }) memoryStream = new MemoryStream(content);
        var contentType = result.responseHeaders["Content-Type"].FirstOrDefault() ?? "application/octet-stream";
        return new ObjectOutPut(input.ObjectName
            , memoryStream ?? Stream.Null
            , contentType);
    }

    public async Task<bool> UploadObjectAsync(UploadObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));

        var found = await BucketExistsAsync(input.BucketName);
        //如果桶不存在则创建桶
        if (!found)
            await CreateBucketAsync(input.BucketName);

        if (string.IsNullOrEmpty(input.ObjectName)) throw new ArgumentNullException(nameof(input.ObjectName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        input.BucketName = ConvertBucketName(input.BucketName);
        var request = new PutObjectRequest(input.BucketName, input.ObjectName, input.Stream);
        var result = Context.PutObject(request);
        if (result.IsSuccessful()) return true;

        return false;
    }

    public Task<bool> PutObjectAsync(string bucketName, string objectName, Stream data,
        CancellationToken cancellationToken = default)
    {
        byte[] StreamToBytes(Stream stream)
        {
            if (stream == null || stream.Length == 0) throw new Exception("Input stream is null");
            var length = stream.Length - stream.Position;
            if (length == 0) throw new Exception("Stream position at end of stream, this stream have no data to read.");
            if (length > int.MaxValue) throw new Exception("The input stream is too long.");
            var position = stream.Position;
            var bytes = new byte[length];
            stream.ReadExactly(bytes, (int)stream.Position, (int)length);
            stream.Position = position;
            return bytes;
        }

        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        var upload = StreamToBytes(data);
        if (upload == null || upload.Length == 0) throw new Exception("Upload file stram is null.");
        var contentType = "application/octet-stream";
        if (data is FileStream fileStream)
        {
            var fileName = fileStream.Name;
            if (!string.IsNullOrEmpty(fileName))
                new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
        }
        else
        {
            new FileExtensionContentTypeProvider().TryGetContentType(objectName, out contentType);
        }

        if (string.IsNullOrEmpty(contentType)) contentType = "application/octet-stream";
        try
        {
            var request = new PostObjectRequest(ConvertBucketName(bucketName), objectName, upload);
            request.SetContentType(contentType);
            var result = Context.PostObject(request);
            return Task.FromResult(result.IsSuccessful());
        }
        catch (CosServerException ex)
        {
            if (ex.statusCode == 404)
                throw new Exception($"Bucket '{ConvertBucketName(bucketName)}' not exists, ex: {ex.errorMessage}");
            throw;
        }
    }

    public Task<bool> PutObjectAsync(string bucketName, string objectName, string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath)) throw new Exception("Upload file is not exist.");
        bucketName = ConvertBucketName(bucketName);
        var request = new PutObjectRequest(bucketName, objectName, filePath);
        var result = Context.PutObject(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<ItemMeta> GetObjectMetadataAsync(string bucketName
        , string objectName
        , string? versionId = null
        , string? matchEtag = null
        , DateTime? modifiedSince = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        bucketName = ConvertBucketName(bucketName);
        var request = new HeadObjectRequest(bucketName, objectName);
        if (!string.IsNullOrEmpty(versionId)) request.SetVersionId(versionId);
        if (!string.IsNullOrEmpty(matchEtag))
        {
            // request.SetIfMatch(matchEtag);
        }

        if (modifiedSince.HasValue)
        {
            // request.SetIfModifiedSince(modifiedSince.Value.ToString("r"));
        }

        //执行请求
        var result = Context.HeadObject(request);
        if (!result.IsSuccessful()) throw new Exception("Query object meta data failed.");
        var metaData = new ItemMeta
        {
            ObjectName = objectName,
            Size = result.size,
            ETag = result.eTag,
            IsEnableHttps = Options.IsEnableHttps
        };
        return Task.FromResult(metaData);
    }

    public Task<bool> CopyObjectAsync(string bucketName, string objectName, string? destBucketName,
        string? destObjectName = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (string.IsNullOrEmpty(destBucketName)) destBucketName = bucketName;
        if (string.IsNullOrEmpty(destObjectName)) destObjectName = objectName;
        destObjectName = FormatObjectName(destObjectName!);
        bucketName = ConvertBucketName(bucketName);
        var copySource = new CopySourceStruct(Options.Endpoint, bucketName, Options.Region, objectName);
        var bucket = ConvertBucketName(destBucketName);
        var request = new CopyObjectRequest(bucket, destObjectName);
        //设置拷贝源
        request.SetCopySource(copySource);
        //设置是否拷贝还是更新,此处是拷贝
        request.SetCopyMetaDataDirective(CosMetaDataDirective.Copy);
        //执行请求
        var result = Context.CopyObject(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<bool> RemoveObjectAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        input.BucketName = ConvertBucketName(input.BucketName);
        var request = new DeleteObjectRequest(input.BucketName, input.ObjectName);
        var result = Context.DeleteObject(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<bool> RemoveObjectAsync(string bucketName, string objectName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        bucketName = ConvertBucketName(bucketName);
        var request = new DeleteObjectRequest(bucketName, objectName);
        var result = Context.DeleteObject(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNames)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        if (objectNames == null || objectNames.Count == 0) throw new ArgumentNullException(nameof(objectNames));
        List<string> delObjects = [];
        foreach (var item in objectNames) delObjects.Add(FormatObjectName(item));
        bucketName = ConvertBucketName(bucketName);
        var request = new DeleteMultiObjectRequest(bucketName);
        //设置返回结果形式
        request.SetDeleteQuiet(false);
        request.SetObjectKeys(delObjects);
        var result = Context.DeleteMultiObjects(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt)
    {
        return PresignedObjectAsync(bucketName
            , objectName
            , expiresInt
            , PresignedObjectType.Get
            , async (bucketName, objectName, expiresInt) =>
            {
                objectName = FormatObjectName(objectName);
                var newBucketName = ConvertBucketName(bucketName);
                var preSignatureStruct = new PreSignatureStruct
                {
                    appid = Options.Endpoint ?? string.Empty,
                    region = Options.Region,
                    bucket = newBucketName,
                    key = objectName,
                    httpMethod = "GET",
                    isHttps = Options.IsEnableHttps,
                    signDurationSecond = expiresInt,
                    headers = null,
                    queryParameters = null
                };
                string? objectUrl = null;
                //生成URL
                var accessMode = await GetObjectAclAsync(new OperateObjectInput
                {
                    BucketName = newBucketName,
                    ObjectName = objectName
                });
                if (accessMode == AccessMode.PublicRead || accessMode == AccessMode.PublicReadWrite)
                {
                    objectUrl =
                        $"{(Options.IsEnableHttps ? "https" : "http")}://{newBucketName}.cos.{Options.Region}.myqcloud.com{(objectName.StartsWith("/") ? "" : "/")}{objectName}";
                }
                else
                {
                    var uri = Context.GenerateSignURL(preSignatureStruct);
                    if (uri != null) objectUrl = uri;
                }

                if (string.IsNullOrEmpty(objectUrl)) throw new Exception("Generate get presigned uri failed");
                return objectUrl;
            });
    }

    public Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
    {
        return PresignedObjectAsync(bucketName
            , objectName
            , expiresInt
            , PresignedObjectType.Put
            , (bucketName, objectName, expiresInt) =>
            {
                objectName = FormatObjectName(objectName);
                var newBucketName = ConvertBucketName(bucketName);
                var preSignatureStruct = new PreSignatureStruct
                {
                    appid = Options.Endpoint,
                    region = Options.Region,
                    bucket = newBucketName,
                    key = objectName,
                    httpMethod = "PUT",
                    isHttps = Options.IsEnableHttps,
                    signDurationSecond = expiresInt,
                    headers = null,
                    queryParameters = null
                };
                var uri = Context.GenerateSignURL(preSignatureStruct);
                if (uri == null) throw new Exception("Generate get presigned uri failed");
                return Task.FromResult(uri);
            });
    }

    public Task<bool> SetObjectAclAsync(string bucketName, string objectName, AccessMode mode)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        bucketName = ConvertBucketName(bucketName);
        var acl = mode switch
        {
            AccessMode.Default => CosACL.Private,
            AccessMode.Private => CosACL.Private,
            AccessMode.PublicRead => CosACL.PublicRead,
            AccessMode.PublicReadWrite => CosACL.PublicReadWrite,
            _ => CosACL.Private
        };
        if (acl == CosACL.PublicReadWrite) throw new Exception("QCloud object not support public read and write.");

        var request = new PutObjectACLRequest(bucketName, objectName);
        //设置私有读写权限 
        request.SetCosACL(acl);
        var result = Context.PutObjectACL(request);
        return Task.FromResult(result.IsSuccessful());
    }

    public async Task<AccessMode> GetObjectAclAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        var isPublicRead = false;
        var isPublicWrite = false;
        var result =
            Context.GetObjectACL(new GetObjectACLRequest(ConvertBucketName(input.BucketName), input.ObjectName));
        var acl = result.accessControlPolicy;

        if (result.responseHeaders.ContainsKey("x-cos-acl")
            && result.responseHeaders["x-cos-acl"] != null
            && result.responseHeaders["x-cos-acl"].Count > 0
            && result.responseHeaders["x-cos-acl"][0].Equals("default"))
        {
            //继承权限,获取储存桶权限
            var bucketMode = await GetBucketAclAsync(input.BucketName);
            switch (bucketMode)
            {
                case AccessMode.PublicRead:
                {
                    isPublicRead = true;
                    isPublicWrite = false;
                    break;
                }
                case AccessMode.PublicReadWrite:
                {
                    isPublicRead = true;
                    isPublicWrite = true;
                    break;
                }
                case AccessMode.Default:
                case AccessMode.Private:
                default:
                {
                    isPublicRead = false;
                    isPublicWrite = false;
                    break;
                }
            }
        }

        if (acl is { accessControlList.grants.Count: > 0 })
            foreach (var item in acl.accessControlList.grants)
            {
                if (string.IsNullOrEmpty(item.grantee.uri)) continue;
                if (item.grantee.uri.Contains("allusers", StringComparison.OrdinalIgnoreCase))
                    switch (item.permission.ToLower())
                    {
                        case "read":
                            isPublicRead = true;
                            break;
                        case "write":
                            isPublicWrite = true;
                            break;
                    }
            }

        //结果
        if (isPublicRead && !isPublicWrite) return await Task.FromResult(AccessMode.PublicRead);

        if (isPublicRead && isPublicWrite) return await Task.FromResult(AccessMode.PublicReadWrite);

        if (!isPublicRead && isPublicWrite) return await Task.FromResult(AccessMode.Private);

        return await Task.FromResult(AccessMode.Private);
    }

    public Task<AccessMode> RemoveObjectAclAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        var request = new PutObjectACLRequest(ConvertBucketName(input.BucketName), input.ObjectName);
        //设置私有读写权限 
        request.SetCosACL("default");
        var result = Context.PutObjectACL(request);
        if (result.IsSuccessful()) return GetObjectAclAsync(input);
        throw new Exception("Remove object acl failed.");
    }

    #endregion
}