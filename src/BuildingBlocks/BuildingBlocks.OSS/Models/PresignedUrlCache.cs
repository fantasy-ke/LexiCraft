namespace BuildingBlocks.OSS.Models;

internal class PresignedUrlCache
{
    public string Name { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public long CreateTime { get; set; }

    public string Url { get; set; } = string.Empty;

    public PresignedObjectType Type { get; set; }
}