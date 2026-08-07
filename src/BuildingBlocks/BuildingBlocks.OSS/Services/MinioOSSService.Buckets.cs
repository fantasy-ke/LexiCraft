using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Exceptions;
using BuildingBlocks.OSS.Models.Policy;
using Minio.DataModel.Args;
using Bucket = BuildingBlocks.OSS.Models.Bucket;

namespace BuildingBlocks.OSS.Services;

public partial class MinioOssService
{
    #region Bucket

    public Task<bool> BucketExistsAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        var args = new BucketExistsArgs().WithBucket(bucketName);
        return Context.BucketExistsAsync(args);
    }

    public async Task<bool> CreateBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        var found = await BucketExistsAsync(bucketName);
        if (found) throw new BucketExistException($"Bucket '{bucketName}' already exists.");

        await Context.MakeBucketAsync(
            new MakeBucketArgs()
                .WithBucket(bucketName)
                .WithLocation(Options.Region));
        return true;
    }

    public async Task<List<Bucket>> ListBucketsAsync()
    {
        var list = await Context.ListBucketsAsync();
        if (list?.Buckets == null) throw new Exception("List buckets failed, result obj is null");

        List<Bucket> result = [];
        foreach (var item in list.Buckets)
            result.Add(new Bucket
            {
                Name = item.Name,
                Location = Options.Region,
                CreationDate = item.CreationDate,
                Owner = new Owner
                {
                    Id = Options.AccessKey,
                    Name = Options.AccessKey
                }
            });

        return result;
    }

    public async Task<bool> RemoveBucketAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        var found = await BucketExistsAsync(bucketName);
        if (!found) return true;

        await Context.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketName));
        return true;
    }

    public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        List<StatementItem> statementItems = [];
        switch (mode)
        {
            case AccessMode.Private:
            {
                statementItems.Add(new StatementItem
                {
                    Effect = "Deny",
                    Principal = new Principal
                    {
                        AWS = ["*"]
                    },
                    Action =
                    [
                        "s3:DeleteObject",
                        "s3:GetObject",
                        "s3:ListBucket",
                        "s3:PutObject"
                    ],
                    Resource =
                    [
                        "arn:aws:s3:::*"
                    ],
                    IsDelete = false
                });

                return SetPolicyAsync(bucketName, statementItems);
            }
            case AccessMode.PublicRead:
            {
                //允许列出和下载
                statementItems.Add(new StatementItem
                {
                    Effect = "Allow",
                    Principal = new Principal
                    {
                        AWS = ["*"]
                    },
                    Action =
                    [
                        "s3:GetObject",
                        "s3:ListBucket"
                    ],
                    Resource =
                    [
                        "arn:aws:s3:::*"
                    ],
                    IsDelete = false
                });
                //禁止删除和修改
                statementItems.Add(new StatementItem
                {
                    Effect = "Deny",
                    Principal = new Principal
                    {
                        AWS = ["*"]
                    },
                    Action =
                    [
                        "s3:DeleteObject",
                        "s3:PutObject"
                    ],
                    Resource =
                    [
                        "arn:aws:s3:::*"
                    ],
                    IsDelete = false
                });
                return SetPolicyAsync(bucketName, statementItems);
            }
            case AccessMode.PublicReadWrite:
            {
                statementItems.Add(new StatementItem
                {
                    Effect = "Allow",
                    Principal = new Principal
                    {
                        AWS = ["*"]
                    },
                    Action =
                    [
                        "s3:DeleteObject",
                        "s3:GetObject",
                        "s3:ListBucket",
                        "s3:PutObject"
                    ],
                    Resource =
                    [
                        "arn:aws:s3:::*"
                    ],
                    IsDelete = false
                });
                return SetPolicyAsync(bucketName, statementItems);
            }
            case AccessMode.Default:
            default:
            {
                return RemovePolicyAsync(bucketName);
            }
        }
    }

    public async Task<AccessMode> GetBucketAclAsync(string bucketName)
    {
        bool FindAction(List<string>? actions, string input)
        {
            return actions is { Count: > 0 } &&
                   actions.Exists(p => p.Equals(input, StringComparison.OrdinalIgnoreCase));
        }

        var info = await GetPolicyAsync(bucketName);

        if (info.Statement.Count == 0) return AccessMode.Private;

        var statements = UnpackResource(info.Statement);

        var isPublicRead = false;
        var isPublicWrite = false;
        foreach (var item in statements)
        {
            if (!IsRootResource(bucketName, item.Resource[0])) continue;

            if (item.Action.Count == 0) continue;

            if (item.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase))
            {
                if (FindAction(item.Action, "*")) return AccessMode.PublicReadWrite;

                if (FindAction(item.Action, "s3:GetObject")) isPublicRead = true;

                if (FindAction(item.Action, "s3:PutObject")) isPublicWrite = true;
            }

            if (isPublicRead && isPublicWrite) return AccessMode.PublicReadWrite;
        }

        //结果
        if (isPublicRead && !isPublicWrite) return AccessMode.PublicRead;

        if (isPublicRead && isPublicWrite) return AccessMode.PublicReadWrite;

        if (!isPublicRead && isPublicWrite) return AccessMode.Private;

        return AccessMode.Private;
    }

    #endregion
}
