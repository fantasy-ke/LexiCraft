using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain;
using BuildingBlocks.Filters;
using LexiCraf.AuthServer.Application.Contract.User;
using LexiCraf.AuthServer.Application.Contract.User.Dto;
using LexiCraf.AuthServer.Application.Contract.Users.Dto;
using LexiCraft.AuthServer.Domain;
using LexiCraft.AuthServer.Domain.Files;
using LexiCraft.AuthServer.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZAnalyzers.Core;
using ZAnalyzers.Core.Attribute;

namespace LexiCraft.AuthServer.Application.Users;

[ZAnalyzers.Core.Attribute.Route("/api/user")]
[Tags("User")]
[Filter(typeof(ResultEndPointFilter))]
public class UserService(
    IRepository<User> userRepository, 
    IRepository<FileInfos> fileRepository, 
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IWebHostEnvironment hostEnvironment) : FantasyApi, IUserService
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
        }).FirstOrDefaultAsync();
    }

    [EndpointSummary("上传头像")]
    [IgnoreAntiforgeryToken]
    public async Task<AvatarUploadResultDto> UploadAvatarAsync(IFormFile avatar)
    {
        var userId = userContext.UserId;
        var user = await userRepository.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        // 确保头像目录存在
        var  appRootPath = Path.Combine("uploads", "avatar");
        var avatarDir = Path.Combine(hostEnvironment.ContentRootPath, appRootPath);
        if (!Directory.Exists(avatarDir))
        {
            Directory.CreateDirectory(avatarDir);
        }

        // 为避免文件名冲突，在文件名前添加时间戳和用户ID
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileExtension = Path.GetExtension(avatar.FileName);
        var fileName = $"{timestamp}_{userId}{fileExtension}";
        var relativePath = Path.Combine(appRootPath, fileName);
        var fullPath = Path.Combine(hostEnvironment.ContentRootPath, relativePath);

        // 保存文件
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        // 计算文件哈希
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
            // 忽略哈希计算错误
        }

        // 记录文件信息到FileInfos表
        var fileInfo = new FileInfos
        {
            FileName = avatar.FileName,
            FilePath = relativePath,
            FullPath = fullPath,
            Extension = fileExtension.TrimStart('.'),
            FileSize = avatar.Length,
            ContentType = avatar.ContentType,
            IsDirectory = false,
            FileHash = fileHash,
            UploadTime = DateTime.Now,
            LastAccessTime = DateTime.Now,
            Description = $"用户 {userContext.UserName} 的头像"
        };

        await fileRepository.InsertAsync(fileInfo);

        // 更新用户头像路径
        // 使用专门的文件API访问
        var avatarUrl = $"/FileDirectly?relativePath={relativePath}";
        user.Avatar = avatarUrl;
        await userRepository.UpdateAsync(user);
        
        await unitOfWork.SaveChangesAsync();

        return new AvatarUploadResultDto
        {
            AvatarUrl = avatarUrl,
            FileId = fileInfo.Id
        };
    }
}