using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Files.Grpc.Model;

/// <summary>
/// 文件信息
/// </summary>
[Table("file-infos")]
public class FileInfos : AuditEntity<Guid>
{
    /// <summary>
    /// 文件名
    /// </summary>
    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// 文件路径（相对于App_Data的路径）
    /// </summary>
    [MaxLength(1000)]
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// 完整物理路径
    /// </summary>
    [MaxLength(1000)]
    public string FullPath { get; set; } = null!;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [MaxLength(50)]
    public string? Extension { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件类型（MIME类型）
    /// </summary>
    [MaxLength(100)]
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
    /// 文件哈希值（用于去重）
    /// </summary>
    [MaxLength(100)]
    public string? FileHash { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// 文件描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 文件标签（用逗号分隔）
    /// </summary>
    [MaxLength(255)]
    public string? Tags { get; set; }

    /// <summary>
    /// 父目录（导航属性）
    /// </summary>
    [ForeignKey("ParentId")]
    public virtual FileInfos? Parent { get; set; }

    /// <summary>
    /// 子文件/文件夹（导航属性）
    /// </summary>
    public virtual ICollection<FileInfos> Children { get; set; } = new List<FileInfos>();
} 