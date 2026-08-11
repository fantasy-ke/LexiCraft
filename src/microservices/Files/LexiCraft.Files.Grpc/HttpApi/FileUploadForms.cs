namespace LexiCraft.Files.Grpc.HttpApi;

/// <summary>
///     单文件上传表单
/// </summary>
public sealed class UploadFileForm
{
    /// <summary>
    ///     上传的文件
    /// </summary>
    public IFormFile File { get; init; } = null!;

    /// <summary>
    ///     父目录ID
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    ///     文件描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     文件标签，多个标签使用逗号分隔
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    ///     逻辑目录
    /// </summary>
    public string? Directory { get; init; }
}

/// <summary>
///     批量文件上传表单，同一批文件共用目录和元数据
/// </summary>
public sealed class BatchUploadFilesForm
{
    /// <summary>
    ///     上传的文件列表
    /// </summary>
    public List<IFormFile> Files { get; init; } = [];

    /// <summary>
    ///     父目录ID
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    ///     文件描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     文件标签，多个标签使用逗号分隔
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    ///     逻辑目录
    /// </summary>
    public string? Directory { get; init; }
}
