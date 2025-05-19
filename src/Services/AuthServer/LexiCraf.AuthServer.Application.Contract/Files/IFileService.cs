using LexiCraf.AuthServer.Application.Contract.Files.Dtos;
using Microsoft.AspNetCore.Http;

namespace LexiCraf.AuthServer.Application.Contract.Files;

/// <summary>
/// 文件服务接口
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="request">上传请求，包含文件和参数</param>
    /// <returns>文件信息</returns>
    Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request);

    /// <summary>
    /// 批量上传文件
    /// </summary>
    /// <param name="filesDto">文件集合</param>
    /// <returns>文件信息列表</returns>
    Task<List<FileInfoDto>> UploadFilesAsync(List<FileUploadRequestDto> filesDto);

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
    
    /// <summary>
    /// 通过相对路径获取文件
    /// </summary>
    /// <param name="relativePath">相对于App_Data的路径，例如："avatar/example.jpg"</param>
    /// <returns>文件流、文件名和内容类型</returns>
    Task<(Stream FileStream, string FileName, string ContentType)> GetFileByPathAsync(string relativePath);

    /// <summary>
    /// 直接获取文件
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    Task<IResult> GetFileDirectlyAsync(string relativePath);
} 