using BuildingBlocks.OSS.Models;

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

/// <summary>
///     内置对象存储提供商名称。
/// </summary>
public static class OSSProviderNames
{
    public const string Minio = nameof(OSSProvider.Minio);
    public const string Aliyun = nameof(OSSProvider.Aliyun);
    public const string QCloud = nameof(OSSProvider.QCloud);

    public static string FromProvider(OSSProvider provider)
    {
        return provider switch
        {
            OSSProvider.Minio => Minio,
            OSSProvider.Aliyun => Aliyun,
            OSSProvider.QCloud => QCloud,
            _ => string.Empty
        };
    }
}

/// <summary>
///     单个对象存储实例的连接配置。
/// </summary>
public class OSSProviderOptions
{
    private string _region = "us-east-1";

    /// <summary>
    ///     自定义提供商类型。为空时使用 <see cref="Provider" /> 对应的内置类型。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     内置对象存储提供商。新提供商优先使用 <see cref="Type" /> 扩展。
    /// </summary>
    public OSSProvider Provider { get; set; }

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

    internal string GetProviderType()
    {
        return string.IsNullOrWhiteSpace(Type)
            ? OSSProviderNames.FromProvider(Provider)
            : Type.Trim();
    }
}

/// <summary>
///     对象存储模块配置。根级连接字段用于兼容旧的单提供商配置。
/// </summary>
public class OSSOptions : OSSProviderOptions
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
    ///     命名对象存储实例。为空时使用根级连接字段作为单提供商配置。
    /// </summary>
    public Dictionary<string, OSSProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
