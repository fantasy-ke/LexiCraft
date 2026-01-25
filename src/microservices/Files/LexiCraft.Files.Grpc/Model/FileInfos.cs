using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Files.Grpc.Model;

/// <summary>
///     文件信息
/// </summary>
[Table("file-infos")]
public class FileInfos : AuditEntity<Guid>
{
    private FileInfos()
    {
    } // For EF Core

    /// <summary>
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="filePath"></param>
    /// <param name="fullPath"></param>
    /// <param name="fileSize"></param>
    /// <param name="contentType"></param>
    /// <param name="isDirectory"></param>
    /// <param name="parentId"></param>
    /// <param name="fileHash"></param>
    public FileInfos(string fileName, string filePath, string fullPath, long fileSize, string? contentType,
        bool isDirectory, Guid? parentId, string? fileHash)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        FilePath = filePath;
        FullPath = fullPath;
        FileSize = fileSize;
        ContentType = contentType;
        IsDirectory = isDirectory;
        ParentId = parentId;
        FileHash = fileHash;
        UploadTime = DateTime.UtcNow;
        Extension = Path.GetExtension(fileName);
    }

    /// <summary>
    ///     文件名
    /// </summary>
    [MaxLength(255)]
    public string FileName { get; private set; } = null!;

    /// <summary>
    ///     文件路径（相对于App_Data的路径）
    /// </summary>
    [MaxLength(1000)]
    public string FilePath { get; private set; } = null!;

    /// <summary>
    ///     完整物理路径
    /// </summary>
    [MaxLength(1000)]
    public string FullPath { get; private set; } = null!;

    /// <summary>
    ///     文件扩展名
    /// </summary>
    [MaxLength(50)]
    public string? Extension { get; private set; }

    /// <summary>
    ///     文件大小（字节）
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    ///     文件类型（MIME类型）
    /// </summary>
    [MaxLength(100)]
    public string? ContentType { get; private set; }

    /// <summary>
    ///     是否为文件夹
    /// </summary>
    public bool IsDirectory { get; private set; }

    /// <summary>
    ///     父目录ID
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    ///     文件哈希值（用于去重）
    /// </summary>
    [MaxLength(100)]
    public string? FileHash { get; private set; }

    /// <summary>
    ///     上传时间
    /// </summary>
    public DateTime UploadTime { get; private set; } = DateTime.Now;

    /// <summary>
    ///     最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; private set; }

    /// <summary>
    ///     下载次数
    /// </summary>
    public int DownloadCount { get; private set; }

    /// <summary>
    ///     文件描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    ///     文件标签（用逗号分隔）
    /// </summary>
    [MaxLength(255)]
    public string? Tags { get; private set; }

    /// <summary>
    ///     父目录（导航属性）
    /// </summary>
    [ForeignKey("ParentId")]
    public virtual FileInfos? Parent { get; private set; }

    /// <summary>
    ///     子文件/文件夹（导航属性）
    /// </summary>
    public virtual ICollection<FileInfos> Children { get; private set; } = new List<FileInfos>();

    /// <summary>
    /// </summary>
    /// <param name="newName"></param>
    public void Rename(string newName)
    {
        FileName = newName;
        Extension = Path.GetExtension(newName);
    }

    /// <summary>
    /// </summary>
    /// <param name="newFilePath"></param>
    /// <param name="newFullPath"></param>
    /// <param name="newParentId"></param>
    public void Move(string newFilePath, string newFullPath, Guid? newParentId)
    {
        FilePath = newFilePath;
        FullPath = newFullPath;
        ParentId = newParentId;
    }

    /// <summary>
    /// </summary>
    public void IncrementDownloadCount()
    {
        DownloadCount++;
        LastAccessTime = DateTime.UtcNow;
    }

    /// <summary>
    /// </summary>
    /// <param name="description"></param>
    /// <param name="tags"></param>
    public void UpdateMetadata(string? description, string? tags)
    {
        Description = description;
        Tags = tags;
    }
}