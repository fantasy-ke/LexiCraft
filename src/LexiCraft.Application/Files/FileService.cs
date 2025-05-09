using System.Linq.Expressions;
using LexiCraft.Application.Contract.Files;
using LexiCraft.Application.Contract.Files.Dtos;
using LexiCraft.Domain;
using LexiCraft.Domain.Files;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using ZAnalyzers.Core;
using ZAnalyzers.Core.Attribute;

namespace LexiCraft.Application.Files;

/// <summary>
/// 文件服务实现
/// </summary>
[ZAnalyzers.Core.Attribute.Route("/api/fileDeal")]
[Tags("File")]
[Filter(typeof(ResultEndPointFilter))]
public class FileService :FantasyApi, IFileService
{
    private readonly IRepository<FileInfos> _fileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ILogger<FileService> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public FileService(
        IRepository<FileInfos> fileRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        IWebHostEnvironment hostEnvironment,
        ILogger<FileService> logger)
    {
        _fileRepository = fileRepository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [EndpointSummary("上传文件")]
    public async Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request)
    {
        if (request.File == null)
        {
            throw new ArgumentException("未提供文件");
        }
        
        // 检查父目录是否存在
        if (request.ParentId.HasValue)
        {
            var parentDir = await _fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            if (parentDir == null)
            {
                throw new Exception($"父目录不存在: {request.ParentId}");
            }
            
            if (!parentDir.IsDirectory)
            {
                throw new Exception($"指定的父目录不是一个目录: {request.ParentId}");
            }
        }

        // 获取上传路径，默认存放在App_Data目录
        var appDataPath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // 为避免文件名冲突，在文件名前添加时间戳
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileName = $"{timestamp}_{request.File.FileName}";
        
        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;
        if (request.ParentId.HasValue)
        {
            var parentDir = await _fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            relativePath = Path.Combine(parentDir!.FilePath, fileName);
        }
        else
        {
            relativePath = fileName;
        }
        
        var fullPath = Path.Combine(appDataPath, relativePath);
        
        // 确保目录存在
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 保存文件
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        // 计算文件哈希（可选，用于后续文件去重）
        string? fileHash = null;
        try
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var hash = await md5.ComputeHashAsync(stream);
            fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "计算文件哈希值失败");
        }

        // 创建文件信息实体
        var fileInfo = new FileInfos
        {
            FileName = request.File.FileName,
            FilePath = relativePath,
            FullPath = fullPath,
            Extension = Path.GetExtension(request.File.FileName)?.TrimStart('.'),
            FileSize = request.File.Length,
            ContentType = request.File.ContentType,
            IsDirectory = false,
            ParentId = request.ParentId,
            FileHash = fileHash,
            UploadTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            Description = request.Description,
            Tags = request.Tags
        };

        // 保存到数据库
        await _fileRepository.InsertAsync(fileInfo);
        await _unitOfWork.SaveChangesAsync();

        return fileInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    /// 批量上传文件
    /// </summary>
    [EndpointSummary("批量上传文件")]
    public async Task<List<FileInfoDto>> UploadFilesAsync(List<FileUploadRequestDto> filesDto)
    {
        var results = new List<FileInfoDto>();
        foreach (var file in filesDto)
        {
            var result = await UploadFileAsync(file);
            results.Add(result);
        }
        return results;
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    [EndpointSummary("创建文件夹")]
    public async Task<FileInfoDto> CreateFolderAsync(CreateFolderDto createFolderDto)
    {
        // 检查父目录是否存在
        if (createFolderDto.ParentId.HasValue)
        {
            var parentDir = await _fileRepository.FirstOrDefaultAsync(f => f.Id == createFolderDto.ParentId);
            if (parentDir == null)
            {
                throw new Exception($"父目录不存在: {createFolderDto.ParentId}");
            }
            
            if (!parentDir.IsDirectory)
            {
                throw new Exception($"指定的父目录不是一个目录: {createFolderDto.ParentId}");
            }
        }

        // 获取上传路径，默认存放在App_Data目录
        var appDataPath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;
        string? parentPath = null;
        
        if (createFolderDto.ParentId.HasValue)
        {
            var parentDir = await _fileRepository.FirstOrDefaultAsync(f => f.Id == createFolderDto.ParentId);
            parentPath = parentDir!.FilePath;
            relativePath = Path.Combine(parentPath, createFolderDto.FolderName);
        }
        else
        {
            relativePath = createFolderDto.FolderName;
        }
        
        var fullPath = Path.Combine(appDataPath, relativePath);
        
        // 创建物理目录
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // 创建文件夹信息实体
        var folderInfo = new FileInfos
        {
            FileName = createFolderDto.FolderName,
            FilePath = relativePath,
            FullPath = fullPath,
            Extension = null,
            FileSize = 0,
            ContentType = "application/x-directory",
            IsDirectory = true,
            ParentId = createFolderDto.ParentId,
            FileHash = null,
            UploadTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            Description = createFolderDto.Description,
            Tags = createFolderDto.Tags
        };

        // 保存到数据库
        await _fileRepository.InsertAsync(folderInfo);
        await _unitOfWork.SaveChangesAsync();

        return folderInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    [EndpointSummary("获取文件信息")]
    public async Task<FileInfoDto> GetFileInfoAsync(Guid id)
    {
        var fileInfo = await _fileRepository.FirstOrDefaultAsync(f => f.Id == id);
        if (fileInfo == null)
        {
            throw new Exception($"文件不存在: {id}");
        }

        return fileInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    /// 查询文件列表
    /// </summary>
    [EndpointSummary("查询文件列表")]
    public async Task<(List<FileInfoDto> Items, int Total)> QueryFilesAsync(FileQueryDto queryDto)
    {
        // 构建查询条件
        Expression<Func<FileInfos, bool>> predicate = f => true;

        // 按目录查询
        if (queryDto.DirectoryId.HasValue)
        {
            predicate = predicate.And(f => f.ParentId == queryDto.DirectoryId);
        }
        else
        {
            // 如果没有指定目录ID，则查询根目录下的文件
            predicate = predicate.And(f => f.ParentId == null);
        }

        // 按文件名查询
        if (!string.IsNullOrWhiteSpace(queryDto.FileName))
        {
            predicate = predicate.And(f => f.FileName.Contains(queryDto.FileName));
        }

        // 按扩展名查询
        if (!string.IsNullOrWhiteSpace(queryDto.Extension))
        {
            predicate = predicate.And(f => f.Extension == queryDto.Extension);
        }

        // 按标签查询
        if (!string.IsNullOrWhiteSpace(queryDto.Tags))
        {
            var tags = queryDto.Tags.Split(',').Select(t => t.Trim()).ToArray();
            // 查找包含任一标签的文件
            predicate = predicate.And(f => tags.Any(tag => f.Tags != null && f.Tags.Contains(tag)));
        }

        // 按文件类型查询
        if (queryDto.FilesOnly == true)
        {
            predicate = predicate.And(f => !f.IsDirectory);
        }
        else if (queryDto.DirectoriesOnly == true)
        {
            predicate = predicate.And(f => f.IsDirectory);
        }

        // 按上传时间查询
        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(f => f.UploadTime >= queryDto.StartTime.Value);
        }
        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(f => f.UploadTime <= queryDto.EndTime.Value);
        }

        // 执行查询并分页
        var result = await _fileRepository.GetPageListAsync(
            predicate,
            queryDto.PageIndex,
            queryDto.PageSize,
            f => f.UploadTime,
            false);

        // 转换结果
        var items = result.result.Adapt<List<FileInfoDto>>();
        return (items, result.total);
    }

    /// <summary>
    /// 删除文件或文件夹
    /// </summary>
    [EndpointSummary("删除文件或文件夹")]
    public async Task<bool> DeleteAsync(Guid id)
    {
        var fileInfo = await _fileRepository.FirstOrDefaultAsync(f => f.Id == id);
        if (fileInfo == null)
        {
            throw new Exception($"文件不存在: {id}");
        }

        // 如果是文件夹，需要递归删除所有子文件和子文件夹
        if (fileInfo.IsDirectory)
        {
            // 获取所有子文件和子文件夹
            var children = await _fileRepository.GetListAsync(f => f.ParentId == id);
            
            // 递归删除所有子项
            foreach (var child in children)
            {
                await DeleteAsync((Guid)child.Id!);
            }
            
            // 删除物理文件夹
            if (Directory.Exists(fileInfo.FullPath))
            {
                Directory.Delete(fileInfo.FullPath, true);
            }
        }
        else
        {
            // 删除物理文件
            if (File.Exists(fileInfo.FullPath))
            {
                File.Delete(fileInfo.FullPath);
            }
        }

        // 删除数据库中的记录
        await _fileRepository.DeleteAsync(fileInfo);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 获取目录树
    /// </summary>
    [EndpointSummary("获取目录树")]
    public async Task<List<FileInfoDto>> GetDirectoryTreeAsync()
    {
        // 获取所有文件夹
        var allDirectories = await _fileRepository.GetListAsync(f => f.IsDirectory);
        
        // 创建根节点列表
        var rootDirectories = allDirectories.Where(d => d.ParentId == null).ToList();
        
        // 递归构建目录树
        var result = rootDirectories.Adapt<List<FileInfoDto>>();
        
        foreach (var rootDir in result)
        {
            await BuildDirectoryTreeAsync(rootDir, allDirectories);
        }
        
        return result;
    }
    
    /// <summary>
    /// 递归构建目录树
    /// </summary>
    private async Task BuildDirectoryTreeAsync(FileInfoDto parent, List<FileInfos> allDirectories)
    {
        parent.Children = allDirectories
            .Where(d => d.ParentId == parent.Id)
            .Adapt<List<FileInfoDto>>();
            
        foreach (var child in parent.Children ?? new List<FileInfoDto>())
        {
            await BuildDirectoryTreeAsync(child, allDirectories);
        }
    }

    /// <summary>
    /// 通过相对路径获取文件
    /// </summary>
    [EndpointSummary("通过相对路径获取文件")]
    public async Task<(Stream FileStream, string FileName, string ContentType)> GetFileByPathAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(relativePath));
        }
        
        // 确保路径安全，防止目录遍历攻击
        if (relativePath.Contains(".."))
        {
            throw new Exception("不允许使用相对路径导航符 '..'");
        }
        
        // 构建完整路径
        var fullPath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data", relativePath);
        
        // 检查文件是否存在
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }
        
        // 获取文件名
        var fileName = Path.GetFileName(fullPath);
        
        // 尝试从数据库中查找文件记录，更新访问时间和下载次数
        var fileInfo = await _fileRepository.FirstOrDefaultAsync(f => f.FilePath == relativePath);
        if (fileInfo != null)
        {
            fileInfo.LastAccessTime = DateTime.Now;
            fileInfo.DownloadCount++;
            await _fileRepository.UpdateAsync(fileInfo);
            await _unitOfWork.SaveChangesAsync();
        }
        
        // 使用FileExtensionContentTypeProvider获取MIME类型
        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        
        // 返回文件流
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (fileStream, fileName, contentType);
    }

    /// <summary>
    /// 直接获取文件（获取到文件流）
    /// </summary>
    [EndpointSummary("直接获取文件")]
    [ActionName("GetFileDirectly")]
    public async Task<IResult> GetFileDirectlyAsync(string relativePath)
    {
        try
        {
            // 调用已有的GetFileByPathAsync方法
            var (fileStream, fileName, contentType) = await GetFileByPathAsync(relativePath);
            
            // 返回文件结果
            return Results.File(fileStream, contentType, enableRangeProcessing: true);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件时出错: {RelativePath}", relativePath);
            return Results.Problem($"获取文件失败: {ex.Message}");
        }
    }
}
