using Microsoft.AspNetCore.Http;

namespace LexiCraf.AuthServer.Application.Contract.Files.Dtos;

/// <summary>
/// 文件上传请求DTO
/// </summary>
public class FileUploadRequestDto
{
    /// <summary>
    /// 上传的文件
    /// </summary>
    public IFormFile File { get; set; } = null!;
    
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