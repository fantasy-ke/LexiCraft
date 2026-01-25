namespace BuildingBlocks.OSS.Models;

public class ItemMeta
{
    public string ObjectName { get; internal set; } = string.Empty;

    public long Size { get; internal set; }

    public DateTime LastModified { get; internal set; }

    public string ETag { get; internal set; } = string.Empty;

    public string ContentType { get; internal set; } = string.Empty;

    public bool IsEnableHttps { get; set; }

    public Dictionary<string, string> MetaData { get; internal set; } = [];
}