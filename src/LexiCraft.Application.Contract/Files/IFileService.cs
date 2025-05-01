using LexiCraft.Application.Contract.Files.Dtos;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Application.Contract.Files;

/// <summary>
/// 文件服务接口
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <param name="fileUploadDto">上传参数</param>
    /// <returns>文件信息</returns>
    Task<FileInfoDto> UploadFileAsync(IFormFile file, FileUploadDto fileUploadDto);

    /// <summary>
    /// 批量上传文件
    /// </summary>
    /// <param name="files">文件集合</param>
    /// <param name="fileUploadDto">上传参数</param>
    /// <returns>文件信息列表</returns>
    Task<List<FileInfoDto>> UploadFilesAsync(IFormFileCollection files, FileUploadDto fileUploadDto);

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="createFolderDto">文件夹创建参数</param>
    /// <returns>文件夹信息</returns>
    Task<FileInfoDto> CreateFolderAsync(CreateFolderDto createFolderDto);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件信息</returns>
    Task<FileInfoDto> GetFileInfoAsync(Guid id);

    /// <summary>
    /// 查询文件列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>文件信息列表和总数</returns>
    Task<(List<FileInfoDto> Items, int Total)> QueryFilesAsync(FileQueryDto queryDto);

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件流信息</returns>
    Task<(Stream FileStream, string FileName, string ContentType)> DownloadFileAsync(Guid id);

    /// <summary>
    /// 删除文件或文件夹
    /// </summary>
    /// <param name="id">文件或文件夹ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 获取目录树
    /// </summary>
    /// <returns>文件夹树结构</returns>
    Task<List<FileInfoDto>> GetDirectoryTreeAsync();
} 