using System.Runtime.Serialization;
using System.ServiceModel;
using Microsoft.AspNetCore.Http;
using ProtoBuf.Grpc;

namespace BuildingBlocks.Grpc.Contracts.FileGrpc;


[DataContract]
public class FileUploadRequestDto
{
    [DataMember(Order = 1)]
    public byte[] FileContent { get; set; } = null!;

    [DataMember(Order = 2)]
    public string FileName { get; set; } = null!;

    [DataMember(Order = 3)]
    public string ContentType { get; set; } = null!;

    [DataMember(Order = 4)]
    public long FileSize { get; set; }

    [DataMember(Order = 5)]
    public Guid? ParentId { get; set; }

    [DataMember(Order = 6)]
    public string? Description { get; set; }

    [DataMember(Order = 7)]
    public string? Tags { get; set; }

    [DataMember(Order = 8)]
    public string Directory { get; set; } = "uploads";
}

[DataContract]
public class FileInfoDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [DataMember(Order = 1)]
    public Guid? Id { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [DataMember(Order = 2)]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// 文件路径（相对于App_Data的路径）
    /// </summary>
    [DataMember(Order = 3)]
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [DataMember(Order = 4)]
    public string? Extension { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [DataMember(Order = 5)]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件类型（MIME类型）
    /// </summary>
    [DataMember(Order = 6)]
    public string? ContentType { get; set; }

    /// <summary>
    /// 是否为文件夹
    /// </summary>
    [DataMember(Order = 7)]
    public bool IsDirectory { get; set; }

    /// <summary>
    /// 父目录ID
    /// </summary>
    [DataMember(Order = 8)]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    [DataMember(Order = 9)]
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [DataMember(Order = 10)]
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    [DataMember(Order = 11)]
    public int DownloadCount { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    [DataMember(Order = 12)]
    public string? Description { get; set; }

    /// <summary>
    /// 文件标签（用逗号分隔）
    /// </summary>
    [DataMember(Order = 13)]
    public string? Tags { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DataMember(Order = 14)]
    public DateTime CreateAt { get; set; }

    /// <summary>
    /// 子文件/文件夹列表
    /// </summary>
    [DataMember(Order = 15)]
    public List<FileInfoDto>? Children { get; set; }
}

[ServiceContract]
public interface IFilesService
{
    [OperationContract]
    Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request,
        CallContext context = default);
}