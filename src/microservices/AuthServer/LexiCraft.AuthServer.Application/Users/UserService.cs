using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Filters;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using LexiCraft.AuthServer.Application.Contract.User.Dto;
using LexiCraft.AuthServer.Application.Contract.Users;
using LexiCraft.AuthServer.Application.Contract.Users.Authorization;
using LexiCraft.AuthServer.Application.Contract.Users.Dto;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZAnalyzers.Core;

namespace LexiCraft.AuthServer.Application.Users;

[ZAnalyzers.Core.Route("/api/user")]
[Tags("User")]
[Filter(typeof(ResultEndPointFilter))]
[ZAuthorize(UsersPermissions.Page)]
public class UserService(
    IUserRepository userRepository, 
    IUserContext userContext,
    IFilesService filesService) : FantasyApi, IUserService
{
    [EndpointSummary("获取用户信息")]
    public async Task<UserInfoDto> GetUserInfo()
    {
        var userId = userContext.UserId;

        return await userRepository.QueryNoTracking<User>().Where(p => p.Id == userId).Select(p => new UserInfoDto
        {
            UserId = p.Id,
            UserName = p.Username,
            Email = p.Email,
            Avatar = p.Avatar,
            Phone = p.Phone,
        }).FirstOrDefaultAsync() ?? new UserInfoDto();
    }

    [EndpointSummary("上传头像")]
    [IgnoreAntiforgeryToken]
    [ZAuthorize(UsersPermissions.UploadAvatar)]
    public async Task<AvatarUploadResultDto> UploadAvatarAsync(IFormFile avatar)
    {
        var userId = userContext.UserId;
        var user = await userRepository.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }
        using var memoryStream = new MemoryStream();
        await avatar.CopyToAsync(memoryStream);
        var requestDto = new FileUploadRequestDto
        {
            FileContent = memoryStream.ToArray(),
            ParentId = null,
            FileName = avatar.FileName,
            ContentType = avatar.ContentType,
            FileSize = avatar.Length,
            Description = $"用户 {userContext.UserName} 的头像",
            Tags = null,
            Directory = "avatar"
        };

        var fileInfoDto = await filesService.UploadFileAsync(requestDto);

       

        // 更新用户头像路径
        // 使用专门的文件API访问
        var avatarUrl = $"/FileDirectly?relativePath={fileInfoDto.FilePath}";
        user.Avatar = avatarUrl;

        return new AvatarUploadResultDto
        {
            AvatarUrl = avatarUrl,
            FileId = fileInfoDto.Id
        };
    }
}