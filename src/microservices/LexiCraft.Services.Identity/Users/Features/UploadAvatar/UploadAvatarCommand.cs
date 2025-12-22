using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Users.Dtos;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Services.Identity.Users.Features.UploadAvatar;

public record UploadAvatarCommand(IFormFile Avatar, Guid UserId) : ICommand<AvatarUploadResultDto>;

public class UploadAvatarCommandHandler(
    IUserRepository userRepository,
    IFilesService filesService) 
    : ICommandHandler<UploadAvatarCommand, AvatarUploadResultDto>
{
    public async Task<AvatarUploadResultDto> Handle(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(u => u.Id == command.UserId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        using var memoryStream = new MemoryStream();
        await command.Avatar.CopyToAsync(memoryStream, cancellationToken);
        var requestDto = new FileUploadRequestDto
        {
            FileContent = memoryStream.ToArray(),
            ParentId = null,
            FileName = command.Avatar.FileName,
            ContentType = command.Avatar.ContentType,
            FileSize = command.Avatar.Length,
            Description = $"用户 {user.Username} 的头像",
            Tags = null,
            Directory = "avatar"
        };

        var fileInfoDto = await filesService.UploadFileAsync(requestDto);

        // 更新用户头像路径
        var avatarUrl = $"/FileDirectly?relativePath={fileInfoDto.FilePath}";
        user.Avatar = avatarUrl;

        return new AvatarUploadResultDto
        {
            AvatarUrl = avatarUrl,
            FileId = fileInfoDto.Id
        };
    }
}