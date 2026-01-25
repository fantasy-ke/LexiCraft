namespace BuildingBlocks.OSS.Models;

public class Item
{
    public string Key { get; internal set; } = string.Empty;

    public string LastModified { get; internal set; } = string.Empty;

    public string ETag { get; internal set; } = string.Empty;

    public ulong Size { get; internal set; }

    public bool IsDir { get; internal set; }

    public string BucketName { get; internal set; } = string.Empty;

    public string? VersionId { get; set; }

    public DateTime? LastModifiedDateTime { get; internal set; }
}