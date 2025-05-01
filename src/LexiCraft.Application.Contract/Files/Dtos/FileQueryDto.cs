namespace LexiCraft.Application.Contract.Files.Dtos;

/// <summary>
/// 文件查询DTO
/// </summary>
public class FileQueryDto
{
    /// <summary>
    /// 目录ID（为空则查询根目录）
    /// </summary>
    public Guid? DirectoryId { get; set; }

    /// <summary>
    /// 文件名（模糊查询）
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// 标签（精确匹配，多个标签用逗号分隔）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 是否只查询文件
    /// </summary>
    public bool? FilesOnly { get; set; }

    /// <summary>
    /// 是否只查询文件夹
    /// </summary>
    public bool? DirectoriesOnly { get; set; }

    /// <summary>
    /// 开始上传时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束上传时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
} 