using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using LexiCraft.Files.Grpc.Model;
using Mapster;
using Microsoft.AspNetCore.StaticFiles;
using ProtoBuf.Grpc;
using static System.Guid;

namespace LexiCraft.Files.Grpc.Services;


public class FilesService(
    ILogger<FilesService> logger,
    IRepository<FileInfos> fileRepository, 
    IUnitOfWork unitOfWork, 
    IWebHostEnvironment hostEnvironment) : IFilesService
{
    private readonly FileExtensionContentTypeProvider contentTypeProvider = new();
    public async Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request, CallContext context = default)
    {
        if (request.FileContent == null)
        {
            throw new ArgumentException("未提供文件");
        }
        
        // 检查父目录是否存在
        if (request.ParentId.HasValue)
        {
            var parentDir = await fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
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
        var appDataPath = Path.Combine(hostEnvironment.ContentRootPath, "uploads", request.Directory);
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // 为避免文件名冲突，在文件名前添加时间戳
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileExtension = Path.GetExtension(request.FileName);
        var fileName = $"{timestamp}_{request.FileName}";
        
        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;
        if (request.ParentId.HasValue)
        {
            var parentDir = await fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
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
            await stream.WriteAsync(request.FileContent, 0, request.FileContent.Length);
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
            logger.LogWarning(ex, "计算文件哈希值失败");
        }

        // 创建文件信息实体
        var fileInfo = new FileInfos
        {
            FileName = request.FileName,
            FilePath = $"{request.Directory}/{relativePath}",
            FullPath = fullPath,
            Extension = fileExtension,
            FileSize = request.FileSize,
            ContentType = request.ContentType,
            IsDirectory = false,
            ParentId = request.ParentId,
            FileHash = fileHash,
            UploadTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            Description = request.Description,
            Tags = request.Tags
        };

        // 保存到数据库
        await fileRepository.InsertAsync(fileInfo);
        await unitOfWork.SaveChangesAsync();

        return fileInfo.Adapt<FileInfoDto>();
    }

    public async Task<List<FileInfoDto>> BatchUploadFileAsync(List<FileUploadRequestDto> request, CallContext context = default)
    {
        var results = new List<FileInfoDto>();
        foreach (var file in request)
        {
            var result = await UploadFileAsync(file);
            results.Add(result);
        }
        return results;
    }

    public async Task<FileInfoDto> CreateFolderAsync(CreateFolderDto request, CallContext context = default)
    {
       // 检查父目录是否存在
        if (request.ParentId.HasValue)
        {
            var parentDir = await fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            if (parentDir == null)
            {
                throw new Exception($"父目录不存在: {request.ParentId}");
            }
            
            if (!parentDir.IsDirectory)
            {
                throw new Exception($"指定的父目录不是一个目录: {request.ParentId}");
            }
        }

        // 获取上传路径，默认存放在目录
        var appDataPath = Path.Combine(hostEnvironment.ContentRootPath, "uploads", request.Directory);
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;
        string? parentPath = null;
        
        if (request.ParentId.HasValue)
        {
            var parentDir = await fileRepository.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            parentPath = parentDir!.FilePath;
            relativePath = Path.Combine(parentPath, request.FolderName);
        }
        else
        {
            relativePath = request.FolderName;
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
            FileName = request.FolderName,
            FilePath =  $"{request.Directory}/{relativePath}",
            FullPath = fullPath,
            Extension = null,
            FileSize = 0,
            ContentType = "application/x-directory",
            IsDirectory = true,
            ParentId = request.ParentId,
            FileHash = null,
            UploadTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            Description = request.Description,
            Tags = request.Tags
        };

        // 保存到数据库
        await fileRepository.InsertAsync(folderInfo);
        await unitOfWork.SaveChangesAsync();

        return folderInfo.Adapt<FileInfoDto>();
    }

    public async Task<FileInfoDto> GetFileInfoAsync(string id, CallContext context = default)
    {
        TryParse(id, out var guid);
        var fileInfo = await fileRepository.FirstOrDefaultAsync(f => f.Id == guid);
        if (fileInfo == null)
        {
            throw new Exception($"文件不存在: {id}");
        }

        return fileInfo.Adapt<FileInfoDto>();
    }

    public async Task<QueryFilesResponseDto> QueryFilesAsync(FileQueryDto queryDto, CallContext context = default)
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
        var result = await fileRepository.GetPageListAsync(
            predicate,
            queryDto.PageIndex,
            queryDto.PageSize,
            f => f.UploadTime,
            false);

        // 转换结果
        var items = result.result.Adapt<List<FileInfoDto>>();
        return new QueryFilesResponseDto
        {
            Items = items,
            Total = result.total
        };
    }

    public async Task<DeleteResponseDto> DeleteAsync(string id, CallContext context = default)
    {
        TryParse(id, out var guid);
        var fileInfo = await fileRepository.FirstOrDefaultAsync(f => f.Id == guid);
        if (fileInfo == null)
        {
            throw new Exception($"文件不存在: {id}");
        }

        // 如果是文件夹，需要递归删除所有子文件和子文件夹
        if (fileInfo.IsDirectory)
        {
            // 获取所有子文件和子文件夹
            var children = await fileRepository.GetListAsync(f => f.ParentId == guid);
            
            // 递归删除所有子项
            foreach (var child in children)
            {
                await DeleteAsync(child.Id.ToString());
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
        await fileRepository.DeleteAsync(fileInfo);
        await unitOfWork.SaveChangesAsync();

        return new  DeleteResponseDto { Success = true };
    }

    public async Task<List<FileInfoDto>> GetDirectoryTreeAsync(CallContext context = default)
    {
        // 获取所有文件夹
        var allDirectories = await fileRepository.GetListAsync(f => f.IsDirectory);
        
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

    public async Task<FileResponseDto> GetFileByPathAsync(string relativePath, CallContext context = default)
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
        var fullPath = Path.Combine(hostEnvironment.ContentRootPath, "uploads", relativePath);
        
        // 检查文件是否存在
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }
        
        // 获取文件名
        var fileName = Path.GetFileName(fullPath);
        
        // 尝试从数据库中查找文件记录，更新访问时间和下载次数
        var fileInfo = await fileRepository.FirstOrDefaultAsync(f => f.FilePath == relativePath);
        fileInfo.LastAccessTime = DateTime.Now;
        fileInfo.DownloadCount++;
        await fileRepository.UpdateAsync(fileInfo);
        await unitOfWork.SaveChangesAsync();

        // 使用FileExtensionContentTypeProvider获取MIME类型
        if (!contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        
        // 返回文件流
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return new FileResponseDto
        {
            FileName = fileName,
            ContentType = contentType,
            FileStream = fileStream
        };
    }

}