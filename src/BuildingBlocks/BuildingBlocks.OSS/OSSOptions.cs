namespace BuildingBlocks.OSS;

public enum OSSProvider
{
    /// <summary>
    ///     无效
    /// </summary>
    Invalid = 0,

    /// <summary>
    ///     Minio自建对象储存
    /// </summary>
    Minio = 1,

    /// <summary>
    ///     阿里云OSS
    /// </summary>
    Aliyun = 2,

    /// <summary>
    ///     腾讯云OSS
    /// </summary>
    QCloud = 3
}

public class OSSOptions
{
    private string _region = "us-east-1";

    /// <summary>
    ///     BucketName
    /// </summary>
    public string DefaultBucket { get; set; } = string.Empty;

    /// <summary>
    ///     枚举，OOS提供商
    /// </summary>
    public OSSProvider Provider { get; set; }

    /// <summary>
    ///     节点
    /// </summary>
    /// <remarks>
    ///     腾讯云中表示AppId
    /// </remarks>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    ///     AccessKey
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    ///     SecretKey
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     地域
    /// </summary>
    public string Region
    {
        get => _region;
        set => _region = string.IsNullOrEmpty(value) ? "us-east-1" : value;
    }

    /// <summary>
    ///     是否启用HTTPS
    /// </summary>
    public bool IsEnableHttps { get; set; } = true;

    /// <summary>
    ///     是否启用缓存，默认缓存在MemeryCache中（可使用自行实现的缓存替代默认缓存）
    ///     在使用之前请评估当前应用的缓存能力能否顶住当前请求
    /// </summary>
    public bool IsEnableCache { get; set; } = false;
}