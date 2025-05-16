namespace LexiCraft.Application.Contract.Files.Dtos;

/// <summary>
/// 文件上传DTO
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// 父目录ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 文件标签（用逗号分隔）
    /// </summary>
    public string? Tags { get; set; }
} 