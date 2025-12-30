namespace BuildingBlocks.Caching.Redis;

/// <summary>
/// 缓存配置选项 (Per-request)
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// absolute expiration time relative to now
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// 是否启用本地缓存 (默认: true)
    /// </summary>
    public bool EnableLocalCache { get; set; } = true;

    /// <summary>
    /// 是否启用 GZip 压缩 (默认: false)
    /// </summary>
    public bool EnableGZip { get; set; } = false;

    /// <summary>
    /// 是否隐藏异常 (默认: false, 发生异常时返回 default)
    /// </summary>
    public bool HideErrors { get; set; } = false;
}
