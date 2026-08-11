using BuildingBlocks.Grpc.Contracts.FileGrpc;
using Microsoft.AspNetCore.Mvc;

namespace LexiCraft.Files.Grpc.HttpApi;

public static class FilesApiConfiguration
{
    private const string Tag = "Files";
    private const string PrefixUri = "api/v{version:apiVersion}/files";

    public static IEndpointRouteBuilder MapFilesApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var filesVersionGroup = endpoints
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var filesGroupV1 = filesVersionGroup
            .MapGroup(PrefixUri)
            .HasApiVersion(1.0);

        filesGroupV1
            .MapPost("upload", UploadFileAsync)
            .WithName("UploadFileV1")
            .WithDisplayName("上传文件")
            .WithSummary("上传单个文件")
            .WithDescription("使用 multipart/form-data 上传一个文件并返回文件信息。")
            .Accepts<UploadFileForm>("multipart/form-data")
            .Produces<FileInfoDto>()
            .DisableAntiforgery();

        filesGroupV1
            .MapPost("batch-upload", BatchUploadFilesAsync)
            .WithName("BatchUploadFilesV1")
            .WithDisplayName("批量上传文件")
            .WithSummary("批量上传文件")
            .WithDescription("使用 multipart/form-data 批量上传文件，同一批文件共用目录和元数据。")
            .Accepts<BatchUploadFilesForm>("multipart/form-data")
            .Produces<List<FileInfoDto>>()
            .DisableAntiforgery();

        filesGroupV1
            .MapPost("folders", CreateFolderAsync)
            .WithName("CreateFolderV1")
            .WithDisplayName("创建文件夹")
            .WithSummary("创建文件夹")
            .WithDescription("创建文件夹并返回文件夹信息。")
            .Produces<FileInfoDto>();

        filesGroupV1
            .MapGet("{id:guid}", GetFileInfoAsync)
            .WithName("GetFileInfoV1")
            .WithDisplayName("获取文件信息")
            .WithSummary("根据ID获取文件信息")
            .WithDescription("根据文件或文件夹ID获取详细信息。")
            .Produces<FileInfoDto>();

        filesGroupV1
            .MapGet("query", QueryFilesAsync)
            .WithName("QueryFilesV1")
            .WithDisplayName("查询文件")
            .WithSummary("分页查询文件和文件夹")
            .WithDescription("根据目录、文件名、扩展名、标签和时间范围分页查询文件。")
            .Produces<QueryFilesResponseDto>();

        filesGroupV1
            .MapDelete("{id:guid}", DeleteAsync)
            .WithName("DeleteFileV1")
            .WithDisplayName("删除文件或文件夹")
            .WithSummary("删除文件或文件夹")
            .WithDescription("根据ID删除文件或文件夹。")
            .Produces<DeleteResponseDto>();

        filesGroupV1
            .MapGet("tree", GetDirectoryTreeAsync)
            .WithName("GetDirectoryTreeV1")
            .WithDisplayName("获取目录树")
            .WithSummary("获取完整目录树")
            .WithDescription("获取全部文件夹组成的目录树。")
            .Produces<List<FileInfoDto>>();

        filesGroupV1
            .MapGet("content", GetFileContentAsync)
            .WithName("GetFileContentV1")
            .WithDisplayName("获取文件内容")
            .WithSummary("根据相对路径获取文件内容")
            .WithDescription("读取指定相对路径的文件并返回二进制文件流。")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
        return endpoints;
    }

    private static async Task<FileInfoDto> UploadFileAsync(
        [FromForm] UploadFileForm request,
        [FromServices] IFilesService filesService,
        CancellationToken cancellationToken)
    {
        var uploadRequest = await CreateUploadRequestAsync(
            request.File,
            request.ParentId,
            request.Description,
            request.Tags,
            request.Directory,
            cancellationToken);

        return await filesService.UploadFileAsync(uploadRequest);
    }

    private static async Task<List<FileInfoDto>> BatchUploadFilesAsync(
        [FromForm] BatchUploadFilesForm request,
        [FromServices] IFilesService filesService,
        CancellationToken cancellationToken)
    {
        var uploadRequests = new List<FileUploadRequestDto>(request.Files.Count);

        foreach (var file in request.Files)
            uploadRequests.Add(await CreateUploadRequestAsync(
                file,
                request.ParentId,
                request.Description,
                request.Tags,
                request.Directory,
                cancellationToken));

        return await filesService.BatchUploadFileAsync(uploadRequests);
    }

    private static Task<FileInfoDto> CreateFolderAsync(
        [FromBody] CreateFolderDto request,
        [FromServices] IFilesService filesService)
    {
        return filesService.CreateFolderAsync(request);
    }

    private static Task<FileInfoDto> GetFileInfoAsync(
        [FromRoute] Guid id,
        [FromServices] IFilesService filesService)
    {
        return filesService.GetFileInfoAsync(id.ToString());
    }

    private static Task<QueryFilesResponseDto> QueryFilesAsync(
        [AsParameters] FileQueryParameters query,
        [FromServices] IFilesService filesService)
    {
        return filesService.QueryFilesAsync(query.ToDto());
    }

    private static Task<DeleteResponseDto> DeleteAsync(
        [FromRoute] Guid id,
        [FromServices] IFilesService filesService)
    {
        return filesService.DeleteAsync(id.ToString());
    }

    private static Task<List<FileInfoDto>> GetDirectoryTreeAsync(
        [FromServices] IFilesService filesService)
    {
        return filesService.GetDirectoryTreeAsync();
    }

    private static async Task<IResult> GetFileContentAsync(
        [FromQuery] string relativePath,
        [FromServices] IFilesService filesService)
    {
        var fileResponse = await filesService.GetFileByPathAsync(relativePath);
        return Results.File(fileResponse.FileStream, fileResponse.ContentType, fileResponse.FileName);
    }

    private static async Task<FileUploadRequestDto> CreateUploadRequestAsync(
        IFormFile file,
        Guid? parentId,
        string? description,
        string? tags,
        string? directory,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return new FileUploadRequestDto
        {
            FileContent = stream.ToArray(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            ParentId = parentId,
            Description = description,
            Tags = tags,
            Directory = directory
        };
    }
}
