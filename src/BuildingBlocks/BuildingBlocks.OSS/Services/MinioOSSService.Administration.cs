using BuildingBlocks.OSS.Models;
using BuildingBlocks.OSS.Models.Policy;
using BuildingBlocks.OSS.Utils;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace BuildingBlocks.OSS.Services;

public partial class MinioOssService
{
    #region Minio自有方法

    /// <summary>
    ///     删除一个未完整上传的对象。
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <param name="objectName">存储桶里的对象名称。</param>
    /// <returns></returns>
    public async Task<bool> RemoveIncompleteUploadAsync(string bucketName, string objectName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        objectName = FormatObjectName(objectName);
        var args = new RemoveIncompleteUploadArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);
        await Context.RemoveIncompleteUploadAsync(args);
        return true;
    }

    /// <summary>
    ///     列出存储桶中未完整上传的对象。
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <returns></returns>
    [Obsolete("Obsolete")]
    public Task<List<ItemUploadInfo>> ListIncompleteUploads(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        var args = new ListIncompleteUploadsArgs()
            .WithBucket(bucketName);
        IObservable<Upload> observable = Context.ListIncompleteUploads(args);

        var isFinish = false;
        List<ItemUploadInfo> result = [];

        observable.Subscribe(
            item =>
            {
                result.Add(new ItemUploadInfo
                {
                    Key = item.Key,
                    Initiated = item.Initiated,
                    UploadId = item.UploadId
                });
            },
            ex =>
            {
                isFinish = true;
                throw new Exception(ex.Message, ex);
            },
            () => { isFinish = true; });
        while (!isFinish) Thread.Sleep(0);

        return Task.FromResult(result);
    }

    /// <summary>
    ///     获取存储桶的权限
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <returns></returns>
    public async Task<PolicyInfo> GetPolicyAsync(string bucketName)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            var args = new GetPolicyArgs()
                .WithBucket(bucketName);
            var policyJson = await Context.GetPolicyAsync(args);
            if (string.IsNullOrEmpty(policyJson)) throw new Exception("Result policy json is null.");

            return JsonUtil.DeserializeObject<PolicyInfo>(policyJson)!;
        }
        catch (MinioException ex)
        {
            if (!string.IsNullOrEmpty(ex.Message) &&
                ex.Message.ToLower().Contains("the bucket policy does not exist"))
                return new PolicyInfo
                {
                    Version = _defaultPolicyVersion,
                    Statement = []
                };

            throw;
        }
    }

    /// <summary>
    ///     设置存储桶的权限
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <param name="statements">权限条目</param>
    /// <returns></returns>
    public async Task<bool> SetPolicyAsync(string bucketName, List<StatementItem> statements)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        if (statements == null || statements.Count == 0) throw new ArgumentNullException(nameof(PolicyInfo));

        var addStatements = statements;
        List<StatementItem> tempStatements = [];
        //获取原有的
        var info = await GetPolicyAsync(bucketName);
        var oldStatements = UnpackResource(info.Statement);

        //解析要添加的条目，将包含多条Resource的条目解析为仅包含一条条目的数据
        statements = UnpackResource(statements);
        //验证要添加的数据
        foreach (var addItem in statements)
        {
            if (!addItem.Effect.Equals("Allow", StringComparison.OrdinalIgnoreCase)
                && !addItem.Effect.Equals("Deny", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Add statement effect only support 'Allow' or 'Deny'.");

            if (addItem.Action == null || addItem.Action.Count == 0)
                throw new Exception("Add statement action can not null");

            if (addItem.Resource == null || addItem.Resource.Count == 0)
                throw new Exception("Add statement resource can not null");

            if (addItem.Principal.AWS.Count == 0)
                addItem.Principal = new Principal
                {
                    AWS = ["*"]
                };
        }

        if (oldStatements.Count == 0)
        {
            //没有Policy数据的情况，新建，修改或删除
            foreach (var addItem in statements)
            {
                //跳过删除
                if (addItem.IsDelete) continue;

                tempStatements.Add(addItem);
            }
        }
        else
        {
            foreach (var addItem in addStatements)
            {
                foreach (var oldItem in oldStatements)
                    //判断已经存在的条目是否包含现有要添加的条目
                    //如果存在条目，则更新；不存在条目，添加进去
                    if ((IsRootResource(bucketName, oldItem.Resource[0]) &&
                         IsRootResource(bucketName, addItem.Resource[0]))
                        || oldItem.Resource[0].Equals(addItem.Resource[0], StringComparison.OrdinalIgnoreCase)
                       )
                        oldItem.IsDelete = true; //就记录标识为删除，不重新添加到待添加列表中

                if (!addItem.IsDelete) tempStatements.Add(addItem);
            }

            foreach (var oldItem in oldStatements)
                if (!oldItem.IsDelete)
                    tempStatements.Add(oldItem);
        }

        //reset info
        info.Version = _defaultPolicyVersion;
        info.Statement = tempStatements;

        var policyJson = JsonUtil.SerializeObject(info);
        await Context.SetPolicyAsync(new SetPolicyArgs()
            .WithBucket(bucketName)
            .WithPolicy(policyJson));
        return true;
    }

    /// <summary>
    ///     移除全部存储桶的权限
    ///     如果要单独移除某个桶的权限，可以使用SetPolicyAsync，并将StatementItem中的IsDelete设置为true
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <returns></returns>
    public async Task<bool> RemovePolicyAsync(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        var args = new RemovePolicyArgs().WithBucket(bucketName);
        await Context.RemovePolicyAsync(args);
        return true;
    }

    public async Task<bool> PolicyExistsAsync(string bucketName, StatementItem statement)
    {
        if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

        if (statement == null
            || string.IsNullOrEmpty(statement.Effect)
            || statement.Action == null || statement.Action.Count == 0
            || statement.Resource == null || statement.Resource.Count == 0)
            throw new ArgumentNullException(nameof(StatementItem));

        var info = await GetPolicyAsync(bucketName);
        if (info.Statement.Count == 0) return false;

        if (statement.Resource.Count > 1) throw new Exception("Only support one resource.");

        foreach (var item in info.Statement)
        {
            var result = true;
            var findSource = false;
            if (item.Resource.Count == 1)
            {
                if ((IsRootResource(bucketName, item.Resource[0]) &&
                     IsRootResource(bucketName, statement.Resource[0]))
                    || item.Resource[0].Equals(statement.Resource[0]))
                    findSource = true;
            }
            else
            {
                foreach (var sourceitem in item.Resource)
                    if (sourceitem.Equals(statement.Resource[0])
                        && item.Effect.Equals(statement.Effect, StringComparison.OrdinalIgnoreCase))
                        findSource = true;
            }

            if (!findSource) continue;
            //验证规则
            if (!item.Effect.Equals(statement.Effect))
                //访问权限
                continue;

            if (item.Action.Count < statement.Action.Count)
                //动作，如果存在的条目数量少于要验证的，false
                continue;

            foreach (var actionItem in statement.Action)
                //验证action
                if (!item.Action.Any(p => p.Equals(actionItem, StringComparison.OrdinalIgnoreCase)))
                    result = false;

            if (result) return result;
        }

        return false;
    }

    #endregion
}
