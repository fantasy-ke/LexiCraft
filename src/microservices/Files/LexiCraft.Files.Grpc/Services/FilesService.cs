using System.Linq.Expressions;
using System.Security.Cryptography;
using BuildingBlocks.Domain;
using BuildingBlocks.Extensions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.OSS;
using BuildingBlocks.OSS.Interface;
using LexiCraft.Files.Grpc.Data;
using LexiCraft.Files.Grpc.Model;
using Mapster;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using static System.Guid;

namespace LexiCraft.Files.Grpc.Services;

/// <summary>
///     文件服务
/// </summary>
public class FilesService : IFilesService
{
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar", ".mp3", ".mp4", ".avi"
    ];

    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private readonly FilesDbContext _fileDbContext;
    private readonly IWebHostEnvironment _hostEnvironment;

    private readonly ILogger<FilesService> _logger;
    private readonly OSSOptions _ossOptions;
    private readonly IOSSService _ossService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileDbContext"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="ossOptions"></param>
    /// <param name="serviceProvider"></param>
    public FilesService(
        ILogger<FilesService> logger,
        FilesDbContext fileDbContext,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment hostEnvironment,
        IOptions<OSSOptions> ossOptions,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _fileDbContext = fileDbContext;
        _unitOfWork = unitOfWork;
        _hostEnvironment = hostEnvironment;
        _ossOptions = ossOptions.Value;
        _ossService = (IOSSService)serviceProvider.GetRequiredService(typeof(IOSSService));
    }

    private bool IsOssEnabled =>
        _ossOptions.Enable
        && !string.IsNullOrWhiteSpace(_ossOptions.DefaultBucket)
        && _ossOptions.Provider != OSSProvider.Invalid;

    /// <summary>
    ///     上传文件
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<FileInfoDto> UploadFileAsync(FileUploadRequestDto request)
    {
        if (request.FileContent == null) throw new ArgumentException("未提供文件");

        FileInfos? parentDir = null;

        // 检查父目录是否存在
        if (request.ParentId.HasValue)
        {
            parentDir = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            if (parentDir == null) throw new Exception($"父目录不存在: {request.ParentId}");

            if (!parentDir.IsDirectory) throw new Exception($"指定的父目录不是一个目录: {request.ParentId}");
        }

        // 获取上传路径，默认存放在App_Data目录
        var appDataPath = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", request.Directory ?? string.Empty);
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);

        // 为避免文件名冲突，在文件名前添加时间戳
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileName = $"{timestamp}_{request.FileName}";

        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;
        if (request.ParentId.HasValue)
        {
            parentDir ??= await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == request.ParentId);

            if (parentDir != null)
                relativePath = Path.Combine(parentDir.FilePath, fileName);
            else
                // Fallback or error
                throw new Exception($"父目录不存在: {request.ParentId}");
        }
        else
        {
            relativePath = fileName;
        }

        var fullPath = Path.Combine(appDataPath, relativePath);

        // 确保目录存在
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        // 保存文件
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await stream.WriteAsync(request.FileContent, 0, request.FileContent.Length);
        }

        // 计算文件哈希（可选，用于后续文件去重）
        string? fileHash = null;
        try
        {
            using var md5 = MD5.Create();
            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            var hash = await md5.ComputeHashAsync(stream);
            fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "计算文件哈希值失败");
        }

        var fileInfo = new FileInfos(
            request.FileName,
            $"{request.Directory}/{relativePath}",
            fullPath,
            request.FileSize,
            request.ContentType,
            false,
            request.ParentId,
            fileHash
        );
        fileInfo.UpdateMetadata(request.Description, request.Tags);

        await UploadToOssAsync(fileInfo, request.FileContent);

        await _fileDbContext.FileInfos.AddAsync(fileInfo);
        await _unitOfWork.SaveChangesAsync();

        return fileInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    ///     批量上传文件
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<List<FileInfoDto>> BatchUploadFileAsync(List<FileUploadRequestDto> request,
        CallContext context = default)
    {
        var results = new List<FileInfoDto>();
        foreach (var file in request)
        {
            var result = await UploadFileAsync(file);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    ///     创建文件夹
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<FileInfoDto> CreateFolderAsync(CreateFolderDto request, CallContext context = default)
    {
        // 检查父目录是否存在
        if (request.ParentId.HasValue)
        {
            var parentDir = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            if (parentDir == null) throw new Exception($"父目录不存在: {request.ParentId}");

            if (!parentDir.IsDirectory) throw new Exception($"指定的父目录不是一个目录: {request.ParentId}");
        }

        var appDataPath = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", request.Directory ?? string.Empty);
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);

        // 创建相对路径，如果有父目录则放到对应目录下
        string relativePath;

        if (request.ParentId.HasValue)
        {
            var parentDir = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == request.ParentId);
            var parentPath = parentDir!.FilePath;
            relativePath = Path.Combine(parentPath, request.FolderName);
        }
        else
        {
            relativePath = request.FolderName;
        }

        var fullPath = Path.Combine(appDataPath, relativePath);

        // 创建物理目录
        if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

        // 创建文件夹信息实体
        var folderInfo = new FileInfos(
            request.FolderName,
            $"{request.Directory}/{relativePath}",
            fullPath,
            0,
            "application/x-directory",
            true,
            request.ParentId,
            null
        );
        folderInfo.UpdateMetadata(request.Description, request.Tags);

        // 保存到数据库
        await _fileDbContext.FileInfos.AddAsync(folderInfo);
        await _unitOfWork.SaveChangesAsync();

        return folderInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    ///     获取文件信息
    /// </summary>
    /// <param name="id"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<FileInfoDto> GetFileInfoAsync(string id, CallContext context = default)
    {
        TryParse(id, out var guid);
        var fileInfo = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == guid);
        if (fileInfo == null) throw new Exception($"文件不存在: {id}");

        return fileInfo.Adapt<FileInfoDto>();
    }

    /// <summary>
    ///     查询文件列表
    /// </summary>
    /// <param name="queryDto"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<QueryFilesResponseDto> QueryFilesAsync(FileQueryDto queryDto, CallContext context = default)
    {
        // 构建查询条件
        Expression<Func<FileInfos, bool>> predicate = f => true;

        // 按目录查询
        if (queryDto.DirectoryId.HasValue)
            predicate = predicate.And(f => f.ParentId == queryDto.DirectoryId);
        else
            // 如果没有指定目录ID，则查询根目录下的文件
            predicate = predicate.And(f => f.ParentId == null);

        // 按文件名查询
        if (!string.IsNullOrWhiteSpace(queryDto.FileName))
            predicate = predicate.And(f => f.FileName.Contains(queryDto.FileName));

        // 按扩展名查询
        if (!string.IsNullOrWhiteSpace(queryDto.Extension))
            predicate = predicate.And(f => f.Extension == queryDto.Extension);

        // 按标签查询
        if (!string.IsNullOrWhiteSpace(queryDto.Tags))
        {
            var tags = queryDto.Tags.Split(',').Select(t => t.Trim()).ToArray();
            // 查找包含任一标签的文件
            predicate = predicate.And(f => tags.Any(tag => f.Tags != null && f.Tags.Contains(tag)));
        }

        // 按文件类型查询
        if (queryDto.FilesOnly == true)
            predicate = predicate.And(f => !f.IsDirectory);
        else if (queryDto.DirectoriesOnly == true) predicate = predicate.And(f => f.IsDirectory);

        // 按上传时间查询
        if (queryDto.StartTime.HasValue) predicate = predicate.And(f => f.UploadTime >= queryDto.StartTime.Value);
        if (queryDto.EndTime.HasValue) predicate = predicate.And(f => f.UploadTime <= queryDto.EndTime.Value);

        // 执行查询并分页
        var query = _fileDbContext.FileInfos.AsQueryable();
        if (queryDto.IsDescending == true)
            query = query.OrderByDescending(f => f.UploadTime);
        else
            query = query.OrderBy(f => f.UploadTime);

        var total = await query.Where(predicate).CountAsync();
        var itemsResult = await query.Where(predicate)
            .Skip((queryDto.PageIndex - 1) * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .ToListAsync();

        var items = itemsResult.Adapt<List<FileInfoDto>>();
        return new QueryFilesResponseDto
        {
            Items = items,
            Total = total
        };
    }

    /// <summary>
    ///     删除文件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<DeleteResponseDto> DeleteAsync(string id, CallContext context = default)
    {
        TryParse(id, out var guid);
        var fileInfo = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.Id == guid);
        if (fileInfo == null) throw new Exception($"文件不存在: {id}");

        // 如果是文件夹，需要递归删除所有子文件和子文件夹
        if (fileInfo.IsDirectory)
        {
            // 获取所有子文件和子文件夹
            var children = await _fileDbContext.FileInfos.Where(f => f.ParentId == guid).ToListAsync();

            // 递归删除所有子项
            foreach (var child in children) await DeleteAsync(child.Id.ToString());

            // 删除物理文件夹
            if (Directory.Exists(fileInfo.FullPath)) Directory.Delete(fileInfo.FullPath, true);
        }
        else
        {
            // 删除物理文件
            if (File.Exists(fileInfo.FullPath)) File.Delete(fileInfo.FullPath);
        }

        if (IsOssEnabled && !fileInfo.IsDirectory)
        {
            var objectName = NormalizeObjectName(fileInfo.FilePath);
            await _ossService.RemoveObjectAsync(_ossOptions.DefaultBucket, [objectName]);
        }

        _fileDbContext.FileInfos.Remove(fileInfo);
        await _unitOfWork.SaveChangesAsync();

        return new DeleteResponseDto { Success = true };
    }

    /// <summary>
    ///     获取目录树
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<List<FileInfoDto>> GetDirectoryTreeAsync(CallContext context = default)
    {
        // 获取所有文件夹
        var allDirectories = await _fileDbContext.FileInfos.Where(f => f.IsDirectory).ToListAsync();

        // 创建根节点列表
        var rootDirectories = allDirectories.Where(d => d.ParentId == null).ToList();

        // 递归构建目录树
        var result = rootDirectories.Adapt<List<FileInfoDto>>();

        foreach (var rootDir in result) await BuildDirectoryTreeAsync(rootDir, allDirectories);

        return result;
    }

    /// <summary>
    ///     获取文件内容
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<FileResponseDto> GetFileByPathAsync(string relativePath, CallContext context = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("文件路径不能为空", nameof(relativePath));

        if (IsOssEnabled)
            try
            {
                return await GetFileFromOssAsync(relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从OSS获取文件失败，回退到本地存储: {Path}", relativePath);
            }

        var uploadsRoot = Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, "uploads"));
        var combinedPath = Path.Combine(uploadsRoot, relativePath.Replace('\\', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("访问被拒绝: 检测到路径遍历");

        var fileExtension = Path.GetExtension(fullPath).ToLowerInvariant();

        if (!AllowedExtensions.Contains(fileExtension))
            throw new UnauthorizedAccessException($"不允许的文件类型: {fileExtension}");

        if (!File.Exists(fullPath)) throw new FileNotFoundException($"文件不存在: {relativePath}");

        var fileName = Path.GetFileName(fullPath);

        var fileInfo = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.FilePath == relativePath);
        if (fileInfo != null)
        {
            fileInfo.IncrementDownloadCount();
            _fileDbContext.FileInfos.Update(fileInfo);
            await _unitOfWork.SaveChangesAsync();
        }

        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return new FileResponseDto
        {
            FileName = fileName,
            ContentType = contentType,
            FileStream = fileStream
        };
    }

    private static string NormalizeObjectName(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    ///     递归构建目录树
    /// </summary>
    private async Task BuildDirectoryTreeAsync(FileInfoDto parent, List<FileInfos> allDirectories)
    {
        parent.Children = allDirectories
            .Where(d => d.ParentId == parent.Id)
            .Adapt<List<FileInfoDto>>();

        foreach (var child in parent.Children ?? []) await BuildDirectoryTreeAsync(child, allDirectories);
    }

    private async Task UploadToOssAsync(FileInfos fileInfo, byte[] content)
    {
        if (!IsOssEnabled) return;

        var objectName = NormalizeObjectName(fileInfo.FilePath);
        using var stream = new MemoryStream(content);
        await _ossService.PutObjectAsync(_ossOptions.DefaultBucket, objectName, stream);
    }

    private async Task<FileResponseDto> GetFileFromOssAsync(string relativePath)
    {
        var objectName = NormalizeObjectName(relativePath);
        var fileExtension = Path.GetExtension(objectName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(fileExtension))
            throw new UnauthorizedAccessException($"不允许的文件类型: {fileExtension}");

        var fileInfo = await _fileDbContext.FileInfos.FirstOrDefaultAsync(f => f.FilePath == relativePath);
        var fileName = fileInfo?.FileName ?? Path.GetFileName(objectName);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";

        var memoryStream = new MemoryStream();

        void WriteStream(Stream stream)
        {
            stream.CopyTo(memoryStream);
        }

        await _ossService.GetObjectAsync(_ossOptions.DefaultBucket, objectName, WriteStream);

        memoryStream.Position = 0;

        if (fileInfo != null)
        {
            fileInfo.IncrementDownloadCount();
            _fileDbContext.FileInfos.Update(fileInfo);
            await _unitOfWork.SaveChangesAsync();
        }

        return new FileResponseDto
        {
            FileName = fileName,
            ContentType = contentType,
            FileStream = memoryStream
        };
    }
}