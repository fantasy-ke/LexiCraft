using BuildingBlocks.Exceptions;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Services.Identity.Users.Features.UploadAvatar;

public record UploadAvatarCommand(IFormFile Avatar, Guid UserId) : ICommand<UploadAvatarResult>;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("用户ID不能为空");
            
        RuleFor(x => x.Avatar)
            .NotNull().WithMessage("头像文件不能为空")
            .Must(IsValidImageFile).WithMessage("上传的文件必须是有效的图片格式")
            .Must(BeWithinSizeLimit).WithMessage("上传的文件大小不能超过5MB");
    }
    
    private bool IsValidImageFile(IFormFile file)
    {
        if (file == null) return true;
        
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
    
    private bool BeWithinSizeLimit(IFormFile file)
    {
        if (file == null) return true;
        
        // 限制文件大小为5MB
        return file.Length <= 5 * 1024 * 1024;
    }
}

public record UploadAvatarResult(
    string AvatarUrl,
    Guid? FileId
);

public class UploadAvatarCommandHandler(
    IUserRepository userRepository,
    IFilesService filesService) 
    : ICommandHandler<UploadAvatarCommand, UploadAvatarResult>
{
    public async Task<UploadAvatarResult> Handle(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(u => u.Id == command.UserId);
        if (user == null)
        {
            ThrowUserFriendlyException.ThrowException("用户不存在");
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

        var avatarPath = fileInfoDto.FilePath;
        user.UpdateAvatar(avatarPath);

        await userRepository.UpdateAsync(user);
        await userRepository.SaveChangesAsync();

        return new UploadAvatarResult(avatarPath, fileInfoDto.Id);
    }
}
