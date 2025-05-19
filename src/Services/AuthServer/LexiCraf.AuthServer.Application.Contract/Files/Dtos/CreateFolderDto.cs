namespace LexiCraf.AuthServer.Application.Contract.Files.Dtos;

/// <summary>
/// 创建文件夹DTO
/// </summary>
public class CreateFolderDto
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string FolderName { get; set; } = null!;

    /// <summary>
    /// 父目录ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 文件夹描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 文件夹标签（用逗号分隔）
    /// </summary>
    public string? Tags { get; set; }
} 