using BuildingBlocks.Domain;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using LexiCraft.Files.Grpc.Model;
using Mapster;
using ProtoBuf.Grpc;

namespace LexiCraft.Files.Grpc.Services;

public class FilesService(
    ILogger<FilesService> logger,
    IRepository<FileInfos> fileRepository, 
    IUnitOfWork unitOfWork, 
    IWebHostEnvironment hostEnvironment) : IFilesService
{
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
        var appDataPath = Path.Combine(hostEnvironment.ContentRootPath, request.Directory);
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
            FilePath = relativePath,
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
}