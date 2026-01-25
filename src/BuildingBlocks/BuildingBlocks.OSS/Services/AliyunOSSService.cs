using System.Globalization;
using Aliyun.OSS;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Interface.Base;
using BuildingBlocks.OSS.Interface.Service;
using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Dto;
using BuildingBlocks.OSS.Models.Exceptions;
using BuildingBlocks.OSS.Models.Policy;
using Bucket = BuildingBlocks.OSS.Models.Bucket;
using Owner = BuildingBlocks.OSS.Models.Owner;

namespace BuildingBlocks.OSS.Services;

public class AliyunOssService : BaseOSSService, IAliyunOssService
{
    public AliyunOssService(ICacheProvider cache
        , OSSOptions options) : base(cache, options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options), "The OSSOptions can not null");
        Context = new OssClient(options.Endpoint, options.AccessKey, options.SecretKey);
    }

    public OssClient Context { get; }

    #region Bucket

    public async Task<List<Bucket>> ListBucketsAsync()
    {
        var buckets = Context.ListBuckets();
        if (buckets == null) return [];
        if (!buckets.Any()) return [];
        var resultList = new List<Bucket>();
        foreach (var item in buckets)
            resultList.Add(new Bucket
            {
                Location = item.Location,
                Name = item.Name,
                Owner = new Owner
                {
                    Name = item.Owner.DisplayName,
                    Id = item.Owner.Id
                },
                CreationDate = item.CreationDate.ToString("yyyy-MM-dd HH:mm:ss")
            });
        return resultList;
    }

    public Task<bool> BucketExistsAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        return Task.FromResult(Context.DoesBucketExist(bucketName));
    }

    public async Task<bool> CreateBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        //检查桶是否存在
        var bucket = (await ListBucketsAsync())?.Where(p => p.Name == bucketName)?.FirstOrDefault();
        if (bucket != null)
        {
            var localtion = Options.Endpoint?.Split('.')[0];
            if (!string.IsNullOrEmpty(localtion) &&
                bucket.Location.Equals(localtion, StringComparison.OrdinalIgnoreCase))
                throw new BucketExistException($"Bucket '{bucketName}' already exists.");

            throw new BucketExistException(
                $"There have a same name bucket '{bucketName}' in other localtion '{bucket.Location}'.");
        }

        var request = new CreateBucketRequest(bucketName)
        {
            //设置存储空间访问权限ACL。
            ACL = CannedAccessControlList.Private,
            //设置数据容灾类型。
            DataRedundancyType = DataRedundancyType.LRS
        };
        // 创建存储空间。
        var result = Context.CreateBucket(request);
        return result != null;
    }

    public Task<bool> RemoveBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        Context.DeleteBucket(bucketName);
        return Task.FromResult(true);
    }

    public Task<string> GetBucketLocationAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        var result = Context.GetBucketLocation(bucketName);
        if (result == null) return Task.FromResult(string.Empty);
        return Task.FromResult(result.Location);
    }

    public Task<bool> SetBucketCorsRequestAsync(string bucketName, List<BucketCorsRule> rules)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        if (rules == null || rules.Count == 0) throw new ArgumentNullException(nameof(rules));
        var request = new SetBucketCorsRequest(bucketName);
        foreach (var item in rules)
        {
            var rule = new CORSRule();
            // 指定允许跨域请求的来源。
            rule.AddAllowedOrigin(item.Origin);
            // 指定允许的跨域请求方法(GET/PUT/DELETE/POST/HEAD)。
            rule.AddAllowedMethod(item.Method.ToString());
            // AllowedHeaders和ExposeHeaders不支持通配符。
            rule.AddAllowedHeader(item.AllowedHeader);
            // 指定允许用户从应用程序中访问的响应头。
            rule.AddExposeHeader(item.ExposeHeader);

            request.AddCORSRule(rule);
        }

        // 设置跨域资源共享规则。
        Context.SetBucketCors(request);
        return Task.FromResult(true);
    }

    public Task<string> GetBucketEndpointAsync(string bucketName)
    {
        var result = Context.GetBucketInfo(bucketName);
        if (result is { Bucket: not null }
            && !string.IsNullOrEmpty(result.Bucket.Name)
            && !string.IsNullOrEmpty(result.Bucket.ExtranetEndpoint))
        {
            var host =
                $"{(Options.IsEnableHttps ? "https://" : "http://")}{result.Bucket.Name}.{result.Bucket.ExtranetEndpoint}";
            return Task.FromResult(host);
        }

        return Task.FromResult(string.Empty);
    }

    /// <summary>
    ///     设置储存桶的访问权限
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        var canned = mode switch
        {
            AccessMode.Default => CannedAccessControlList.Default,
            AccessMode.Private => CannedAccessControlList.Private,
            AccessMode.PublicRead => CannedAccessControlList.PublicRead,
            AccessMode.PublicReadWrite => CannedAccessControlList.PublicReadWrite,
            _ => CannedAccessControlList.Default
        };
        var request = new SetBucketAclRequest(bucketName, canned);
        Context.SetBucketAcl(request);
        return Task.FromResult(true);
    }

    /// <summary>
    ///     获取储存桶的访问权限
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    public Task<AccessMode> GetBucketAclAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        var result = Context.GetBucketAcl(bucketName);
        var mode = result.ACL switch
        {
            CannedAccessControlList.Default => AccessMode.Default,
            CannedAccessControlList.Private => AccessMode.Private,
            CannedAccessControlList.PublicRead => AccessMode.PublicRead,
            CannedAccessControlList.PublicReadWrite => AccessMode.PublicReadWrite,
            _ => AccessMode.Default
        };
        return Task.FromResult(mode);
    }

    #endregion

    #region Object

    public Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        var obj = Context.GetObject(bucketName, objectName);
        callback(obj.Content);
        return Task.CompletedTask;
    }

    public Task GetObjectAsync(string bucketName, string objectName, string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
        var fullPath = Path.GetFullPath(fileName);
        var parentPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath)) Directory.CreateDirectory(parentPath);
        objectName = FormatObjectName(objectName);
        return GetObjectAsync(bucketName, objectName, stream =>
        {
            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
            stream.Dispose();
            fs.Close();
        }, cancellationToken);
    }


    public async Task<ObjectOutPut> GetObjectAsync(GetObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        var request = new GetObjectMetadataRequest(input.BucketName, input.ObjectName)
        {
            VersionId = null
        };
        var oldMeta = Context.GetObjectMetadata(request);
        var obj = Context.GetObject(input.BucketName, input.ObjectName);
        return new ObjectOutPut(input.ObjectName
            , obj.Content
            , oldMeta.ContentType);
    }

    public Task<List<Item>> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        List<Item> result = [];
        ObjectListing? resultObj = null;
        var nextMarker = string.Empty;
        do
        {
            // 每页列举的文件个数通过maxKeys指定，超过指定数将进行分页显示。
            var listObjectsRequest = new ListObjectsRequest(bucketName)
            {
                Marker = nextMarker,
                MaxKeys = 100,
                Prefix = prefix
            };
            resultObj = Context.ListObjects(listObjectsRequest);
            if (resultObj == null) break;
            foreach (var item in resultObj.ObjectSummaries)
                result.Add(new Item
                {
                    Key = item.Key,
                    LastModified = item.LastModified.ToString(CultureInfo.InvariantCulture),
                    ETag = item.ETag,
                    Size = (ulong)item.Size,
                    BucketName = bucketName,
                    IsDir = !string.IsNullOrWhiteSpace(item.Key) && item.Key[^1] == '/',
                    LastModifiedDateTime = item.LastModified
                });
            nextMarker = resultObj.NextMarker;
        } while (resultObj?.IsTruncated == true);

        return Task.FromResult(result);
    }

    public Task<bool> ObjectsExistsAsync(string bucketName, string objectName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        return Task.FromResult(Context.DoesObjectExist(bucketName, objectName));
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
                //生成URL
                var accessMode = await GetObjectAclAsync(new OperateObjectInput
                {
                    ObjectName = objectName,
                    BucketName = bucketName
                });
                if (accessMode == AccessMode.PublicRead || accessMode == AccessMode.PublicReadWrite)
                {
                    var bucketUrl = await GetBucketEndpointAsync(bucketName);
                    var uri = $"{bucketUrl}{(objectName.StartsWith("/") ? "" : "/")}{objectName}";
                    return uri;
                }
                else
                {
                    var req = new GeneratePresignedUriRequest(bucketName, objectName, SignHttpMethod.Get)
                    {
                        Expiration = DateTime.Now.AddSeconds(expiresInt)
                    };
                    var uri = Context.GeneratePresignedUri(req);
                    if (uri == null) throw new Exception("Generate get presigned uri failed");
                    return uri.ToString();
                }
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
                var req = new GeneratePresignedUriRequest(bucketName, objectName, SignHttpMethod.Put)
                {
                    Expiration = DateTime.Now.AddSeconds(expiresInt)
                };
                var uri = Context.GeneratePresignedUri(req);
                if (uri == null) throw new Exception("Generate put presigned uri failed");
                return Task.FromResult(uri.ToString());
            });
    }

    public Task<bool> PutObjectAsync(string bucketName, string objectName, Stream data,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        var result = Context.PutObject(bucketName, objectName, data);
        if (result != null) return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public Task<bool> PutObjectAsync(string bucketName, string objectName, string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (!File.Exists(filePath)) throw new Exception("Upload file is not exist.");
        var result = Context.PutObject(bucketName, objectName, filePath);
        if (result != null) return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public async Task<bool> UploadObjectAsync(UploadObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));

        var found = await BucketExistsAsync(input.BucketName);
        //如果桶不存在则创建桶
        if (!found)
            await CreateBucketAsync(input.BucketName);
        input.ObjectName = FormatObjectName(input.ObjectName);

        var result = Context.PutObject(input.BucketName, input.ObjectName, input.Stream);
        if (result != null) return true;

        return false;
    }

    /// <summary>
    ///     文件拷贝，默认采用分片拷贝的方式
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <param name="destBucketName"></param>
    /// <param name="destObjectName"></param>
    /// <returns></returns>
    public Task<bool> CopyObjectAsync(string bucketName, string objectName, string? destBucketName,
        string? destObjectName = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (string.IsNullOrEmpty(destBucketName)) destBucketName = bucketName;
        destObjectName = FormatObjectName(destObjectName ?? objectName);
        var partSize = 50 * 1024 * 1024;
        // 创建OssClient实例。
        // 初始化拷贝任务。可以通过InitiateMultipartUploadRequest指定目标文件元信息。
        var request = new InitiateMultipartUploadRequest(destBucketName, destObjectName);
        var result = Context.InitiateMultipartUpload(request);
        // 计算分片数。
        var metadata = Context.GetObjectMetadata(bucketName, objectName);
        var fileSize = metadata.ContentLength;
        var partCount = (int)fileSize / partSize;
        if (fileSize % partSize != 0) partCount++;
        // 开始分片拷贝。
        var partETags = new List<PartETag>();
        for (var i = 0; i < partCount; i++)
        {
            var skipBytes = (long)partSize * i;
            var size = partSize < fileSize - skipBytes ? partSize : fileSize - skipBytes;
            // 创建UploadPartCopyRequest。可以通过UploadPartCopyRequest指定限定条件。
            var uploadPartCopyRequest =
                new UploadPartCopyRequest(destBucketName, destObjectName, bucketName, objectName, result.UploadId)
                {
                    PartSize = size,
                    PartNumber = i + 1,
                    // BeginIndex用来定位此次上传分片开始所对应的位置。
                    BeginIndex = skipBytes
                };
            // 调用uploadPartCopy方法来拷贝每一个分片。
            var uploadPartCopyResult = Context.UploadPartCopy(uploadPartCopyRequest);
            partETags.Add(uploadPartCopyResult.PartETag);
        }

        // 完成分片拷贝。
        var completeMultipartUploadRequest =
            new CompleteMultipartUploadRequest(destBucketName, destObjectName, result.UploadId);
        // partETags为分片上传中保存的partETag的列表，OSS收到用户提交的此列表后，会逐一验证每个数据分片的有效性。全部验证通过后，OSS会将这些分片合成一个完整的文件。
        foreach (var partETag in partETags) completeMultipartUploadRequest.PartETags.Add(partETag);
        Context.CompleteMultipartUpload(completeMultipartUploadRequest);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveObjectAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        var result = Context.DeleteObject(input.BucketName, input.ObjectName);
        if (result != null) return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNames)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        if (objectNames == null || objectNames.Count == 0) throw new ArgumentNullException(nameof(objectNames));
        List<string> delObjects = [];
        foreach (var item in objectNames) delObjects.Add(FormatObjectName(item));
        var quietMode = false;
        // DeleteObjectsRequest的第三个参数指定返回模式。
        var request = new DeleteObjectsRequest(bucketName, delObjects, quietMode);
        // 删除多个文件。
        var result = Context.DeleteObjects(request);
        if (!quietMode && result.Keys != null)
        {
            if (result.Keys.Count() == delObjects.Count) return Task.FromResult(true);

            throw new Exception("Some file delete failed.");
        }

        if (result != null) return Task.FromResult(true);

        return Task.FromResult(true);
    }

    public async Task<ItemMeta> GetObjectMetadataAsync(string bucketName
        , string objectName
        , string? versionId = null
        , string? matchEtag = null
        , DateTime? modifiedSince = null)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        var request = new GetObjectMetadataRequest(bucketName, objectName)
        {
            VersionId = versionId
            // MatchingETag = matchEtag,
            // ModifiedSinceConstraint = modifiedSince
        };
        var result = Context.GetObjectMetadata(request);
        var meta = new ItemMeta
        {
            ObjectName = objectName,
            Size = result.ContentLength,
            LastModified = result.LastModified,
            ETag = result.ETag,
            ContentType = result.ContentType,
            IsEnableHttps = Options.IsEnableHttps,
            MetaData = new Dictionary<string, string>()
        };
        if (result.UserMetadata is { Count: > 0 })
            foreach (var item in result.UserMetadata)
                meta.MetaData.Add(item.Key, item.Value);

        return meta;
    }

    public async Task<bool> SetObjectAclAsync(string bucketName, string objectName, AccessMode mode)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
        objectName = FormatObjectName(objectName);
        if (!await ObjectsExistsAsync(bucketName, objectName))
            throw new Exception($"Object '{objectName}' not in bucket '{bucketName}'.");
        var canned = mode switch
        {
            AccessMode.Default => CannedAccessControlList.Default,
            AccessMode.Private => CannedAccessControlList.Private,
            AccessMode.PublicRead => CannedAccessControlList.PublicRead,
            AccessMode.PublicReadWrite => CannedAccessControlList.PublicReadWrite,
            _ => CannedAccessControlList.Default
        };
        Context.SetObjectAcl(bucketName, objectName, canned);
        return await Task.FromResult(true);
    }

    public async Task<AccessMode> GetObjectAclAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        if (!await ObjectsExistsAsync(input.BucketName, input.ObjectName))
            throw new Exception($"Object '{input.ObjectName}' not in bucket '{input.BucketName}'.");
        var result = Context.GetObjectAcl(input.BucketName, input.ObjectName);
        var mode = result.ACL switch
        {
            CannedAccessControlList.Default => AccessMode.Default,
            CannedAccessControlList.Private => AccessMode.Private,
            CannedAccessControlList.PublicRead => AccessMode.PublicRead,
            CannedAccessControlList.PublicReadWrite => AccessMode.PublicReadWrite,
            _ => AccessMode.Default
        };
        if (mode == AccessMode.Default) return await GetBucketAclAsync(input.BucketName);
        return await Task.FromResult(mode);
    }

    public async Task<AccessMode> RemoveObjectAclAsync(OperateObjectInput input)
    {
        if (string.IsNullOrEmpty(input.BucketName)) throw new ArgumentNullException(nameof(input.BucketName));
        input.ObjectName = FormatObjectName(input.ObjectName);
        if (!await SetObjectAclAsync(input.BucketName, input.ObjectName, AccessMode.Default))
            throw new Exception("Save new policy info failed when remove object acl.");
        return await GetObjectAclAsync(input);
    }

    #endregion
}