namespace Fantasy.Files.Grpc.Options;

/// <summary>
///     配置文件服务在存储命名迁移期间的只读兼容入口。
/// </summary>
public sealed class FilesStorageCompatibilityOptions
{
    /// <summary>
    ///     配置节名称。
    /// </summary>
    public const string SectionName = "FilesStorageCompatibility";

    /// <summary>
    ///     获取或设置历史 OSS bucket。新对象始终写入当前 OSS Provider 的默认 bucket。
    /// </summary>
    public string? LegacyBucket { get; set; }
}
