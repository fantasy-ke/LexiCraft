using System.Diagnostics.CodeAnalysis;

namespace BuildingBlocks.OSS.Models.Dto;

public class UploadObjectInput
{
    public UploadObjectInput()
    {
    }

    [SetsRequiredMembers]
    public UploadObjectInput(string bucketName, string objectName, string contentType, Stream stream)
    {
        BucketName = bucketName;
        ObjectName = objectName;
        ContentType = contentType;
        Stream = stream;
    }

    /// <summary>
    ///     命名空间
    /// </summary>
    public required string BucketName { get; set; }

    /// <summary>
    ///     对象名称
    /// </summary>
    public required string ObjectName { get; set; }

    /// <summary>
    ///     文件类型
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    ///     文件流
    /// </summary>
    public required Stream Stream { get; set; }

    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}