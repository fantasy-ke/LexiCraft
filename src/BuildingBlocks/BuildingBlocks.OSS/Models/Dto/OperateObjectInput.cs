namespace BuildingBlocks.OSS.Models.Dto;

public class OperateObjectInput
{
    /// <summary>
    ///     存储桶名称
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    ///     存储对象名称
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;
}