using BuildingBlocks.Grpc.Contracts.FileGrpc;

namespace LexiCraft.Files.Grpc.HttpApi;

/// <summary>
///     文件分页查询参数
/// </summary>
public sealed class FileQueryParameters
{
    /// <summary>
    ///     目录ID，为空时查询根目录
    /// </summary>
    public Guid? DirectoryId { get; init; }

    /// <summary>
    ///     文件名，支持模糊查询
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    ///     文件扩展名
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    ///     文件标签，多个标签使用逗号分隔
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    ///     是否仅查询文件
    /// </summary>
    public bool? FilesOnly { get; init; }

    /// <summary>
    ///     是否仅查询文件夹
    /// </summary>
    public bool? DirectoriesOnly { get; init; }

    /// <summary>
    ///     开始上传时间
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    ///     结束上传时间
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    ///     页码，默认1
    /// </summary>
    public int? PageIndex { get; init; }

    /// <summary>
    ///     每页大小，默认20
    /// </summary>
    public int? PageSize { get; init; }

    /// <summary>
    ///     是否按上传时间降序，默认true
    /// </summary>
    public bool? IsDescending { get; init; }

    internal FileQueryDto ToDto()
    {
        return new FileQueryDto
        {
            DirectoryId = DirectoryId,
            FileName = FileName,
            Extension = Extension,
            Tags = Tags,
            FilesOnly = FilesOnly,
            DirectoriesOnly = DirectoriesOnly,
            StartTime = StartTime,
            EndTime = EndTime,
            PageIndex = PageIndex ?? 1,
            PageSize = PageSize ?? 20,
            IsDescending = IsDescending
        };
    }
}
