namespace LexiCraft.Application.Contract.Files.Dtos;

/// <summary>
/// 文件信息DTO
/// </summary>
public class FileInfoDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// 文件路径（相对于App_Data的路径）
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件类型（MIME类型）
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 是否为文件夹
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// 父目录ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 文件标签（用逗号分隔）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 子文件/文件夹列表
    /// </summary>
    public List<FileInfoDto>? Children { get; set; }
} 