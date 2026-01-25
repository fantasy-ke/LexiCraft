using System.Runtime.Serialization;
using System.ServiceModel;
using ProtoBuf.Grpc;

namespace BuildingBlocks.Grpc.Contracts.FileGrpc;

[DataContract]
public class FileUploadRequestDto
{
    [DataMember(Order = 1)] public byte[] FileContent { get; set; } = null!;

    [DataMember(Order = 2)] public string FileName { get; set; } = null!;

    [DataMember(Order = 3)] public string ContentType { get; set; } = null!;

    [DataMember(Order = 4)] public long FileSize { get; set; }

    [DataMember(Order = 5)] public Guid? ParentId { get; set; }

    [DataMember(Order = 6)] public string? Description { get; set; }

    [DataMember(Order = 7)] public string? Tags { get; set; }

    [DataMember(Order = 8)] public string? Directory { get; set; }
}

[DataContract]
public class FileInfoDto
{
    /// <summary>
    ///     文件ID
    /// </summary>
    [DataMember(Order = 1)]
    public Guid? Id { get; set; }

    /// <summary>
    ///     文件名
    /// </summary>
    [DataMember(Order = 2)]
    public string FileName { get; set; } = null!;

    /// <summary>
    ///     文件路径（相对于App_Data的路径）
    /// </summary>
    [DataMember(Order = 3)]
    public string FilePath { get; set; } = null!;

    /// <summary>
    ///     文件扩展名
    /// </summary>
    [DataMember(Order = 4)]
    public string? Extension { get; set; }

    /// <summary>
    ///     文件大小（字节）
    /// </summary>
    [DataMember(Order = 5)]
    public long FileSize { get; set; }

    /// <summary>
    ///     文件类型（MIME类型）
    /// </summary>
    [DataMember(Order = 6)]
    public string? ContentType { get; set; }

    /// <summary>
    ///     是否为文件夹
    /// </summary>
    [DataMember(Order = 7)]
    public bool IsDirectory { get; set; }

    /// <summary>
    ///     父目录ID
    /// </summary>
    [DataMember(Order = 8)]
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     上传时间
    /// </summary>
    [DataMember(Order = 9)]
    public DateTime UploadTime { get; set; }

    /// <summary>
    ///     最后访问时间
    /// </summary>
    [DataMember(Order = 10)]
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    ///     下载次数
    /// </summary>
    [DataMember(Order = 11)]
    public int DownloadCount { get; set; }

    /// <summary>
    ///     文件描述
    /// </summary>
    [DataMember(Order = 12)]
    public string? Description { get; set; }

    /// <summary>
    ///     文件标签（用逗号分隔）
    /// </summary>
    [DataMember(Order = 13)]
    public string? Tags { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    [DataMember(Order = 14)]
    public DateTime CreateAt { get; set; }

    /// <summary>
    ///     子文件/文件夹列表
    /// </summary>
    [DataMember(Order = 15)]
    public List<FileInfoDto>? Children { get; set; }
}

[DataContract]
public class CreateFolderDto
{
    /// <summary>
    ///     文件夹名称
    /// </summary>
    [DataMember(Order = 1)]
    public string FolderName { get; set; } = null!;

    /// <summary>
    ///     父目录ID
    /// </summary>
    [DataMember(Order = 2)]
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     文件夹描述
    /// </summary>
    [DataMember(Order = 3)]
    public string? Description { get; set; }

    /// <summary>
    ///     文件夹标签（用逗号分隔）
    /// </summary>
    [DataMember(Order = 4)]
    public string? Tags { get; set; }

    [DataMember(Order = 5)] public string? Directory { get; set; }
}

[DataContract]
public class FileQueryDto
{
    /// <summary>
    ///     目录ID（为空则查询根目录）
    /// </summary>
    [DataMember(Order = 1)]
    public Guid? DirectoryId { get; set; }

    /// <summary>
    ///     文件名（模糊查询）
    /// </summary>
    [DataMember(Order = 2)]
    public string? FileName { get; set; }

    /// <summary>
    ///     文件扩展名
    /// </summary>
    [DataMember(Order = 3)]
    public string? Extension { get; set; }

    /// <summary>
    ///     标签（精确匹配，多个标签用逗号分隔）
    /// </summary>
    [DataMember(Order = 4)]
    public string? Tags { get; set; }

    /// <summary>
    ///     是否只查询文件
    /// </summary>
    [DataMember(Order = 5)]
    public bool? FilesOnly { get; set; }

    /// <summary>
    ///     是否只查询文件夹
    /// </summary>
    [DataMember(Order = 6)]
    public bool? DirectoriesOnly { get; set; }

    /// <summary>
    ///     开始上传时间
    /// </summary>
    [DataMember(Order = 7)]
    public DateTime? StartTime { get; set; }

    /// <summary>
    ///     结束上传时间
    /// </summary>
    [DataMember(Order = 8)]
    public DateTime? EndTime { get; set; }

    /// <summary>
    ///     页码
    /// </summary>
    [DataMember(Order = 9)]
    public int PageIndex { get; set; } = 1;

    /// <summary>
    ///     每页大小
    /// </summary>
    [DataMember(Order = 10)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    ///     是否降序
    /// </summary>
    [DataMember(Order = 11)]
    public bool? IsDescending { get; set; }
}

[DataContract]
public class QueryFilesResponseDto
{
    [DataMember(Order = 1)] public List<FileInfoDto> Items { get; set; } = new();

    [DataMember(Order = 2)] public int Total { get; set; }
}

[DataContract]
public class DeleteResponseDto
{
    [DataMember(Order = 1)] public bool Success { get; set; }

    [DataMember(Order = 2)] public string? ErrorMessage { get; set; }
}

[DataContract]
public class FileResponseDto
{
    [DataMember(Order = 1)] public string FileName { get; set; } = null!;

    [DataMember(Order = 2)] public string ContentType { get; set; } = null!;

    [DataMember(Order = 3)] public Stream FileStream { get; set; } = null!;
}

[ServiceContract]
public interface IFilesService
{
    /// <summary>
    ///     上传文件
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [OperationContract]
    Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request);

    /// <summary>
    ///     批量上传
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [OperationContract]
    Task<List<FileInfoDto>> BatchUploadFileAsync(List<FileUploadRequestDto> request,
        CallContext context = default);

    /// <summary>
    ///     创建文件夹
    /// </summary>
    /// <returns>文件夹信息</returns>
    [OperationContract]
    Task<FileInfoDto> CreateFolderAsync(CreateFolderDto request, CallContext context = default);

    /// <summary>
    ///     获取文件信息
    /// </summary>
    [OperationContract]
    Task<FileInfoDto> GetFileInfoAsync(string id, CallContext context = default);

    /// <summary>
    ///     查询文件列表
    /// </summary>
    [OperationContract]
    Task<QueryFilesResponseDto> QueryFilesAsync(FileQueryDto queryDto, CallContext context = default);

    /// <summary>
    ///     删除文件或文件夹
    /// </summary>
    [OperationContract]
    Task<DeleteResponseDto> DeleteAsync(string id, CallContext context = default);

    /// <summary>
    ///     获取目录树
    /// </summary>
    [OperationContract]
    Task<List<FileInfoDto>> GetDirectoryTreeAsync(CallContext context = default);

    /// <summary>
    ///     直接获取文件
    /// </summary>
    [OperationContract]
    Task<FileResponseDto> GetFileByPathAsync(string relativePath, CallContext context = default);
}