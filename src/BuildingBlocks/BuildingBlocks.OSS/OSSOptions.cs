using BuildingBlocks.OSS.Models;

namespace BuildingBlocks.OSS;

/// <summary>
///     内置对象存储提供商类型名称。
/// </summary>
public static class OSSProviderNames
{
    public const string Minio = "Minio";
    public const string Aliyun = "Aliyun";
    public const string QCloud = "QCloud";
}

/// <summary>
///     单个命名对象存储实例的连接配置。
/// </summary>
public class OSSProviderOptions
{
    private string _region = "us-east-1";

    /// <summary>
    ///     提供商类型，用于匹配已注册的对象存储实现。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     默认存储桶名称。
    /// </summary>
    public string DefaultBucket { get; set; } = string.Empty;

    /// <summary>
    ///     服务节点。腾讯云配置中表示 AppId。
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    ///     AccessKey。
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    ///     SecretKey。
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     地域。
    /// </summary>
    public string Region
    {
        get => _region;
        set => _region = string.IsNullOrWhiteSpace(value) ? "us-east-1" : value;
    }

    /// <summary>
    ///     是否启用 HTTPS。
    /// </summary>
    public bool IsEnableHttps { get; set; } = true;

    /// <summary>
    ///     是否启用预签名 URL 缓存。
    /// </summary>
    public bool IsEnableCache { get; set; }
}

/// <summary>
///     对象存储模块配置。
/// </summary>
public class OSSOptions
{
    /// <summary>
    ///     是否启用对象存储模块。
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    ///     默认提供商实例名称。
    /// </summary>
    public string DefaultProvider { get; set; } = DefaultOptionName.Name;

    /// <summary>
    ///     命名对象存储实例。
    /// </summary>
    public Dictionary<string, OSSProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
