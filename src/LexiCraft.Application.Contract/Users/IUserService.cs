﻿using LexiCraft.Application.Contract.User.Dto;
using LexiCraft.Application.Contract.Users.Dto;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Application.Contract.User;

public interface IUserService
{
    /// <summary>
    /// 当前登录用户信息
    /// </summary>
    /// <returns></returns>
    Task<UserInfoDto> GetUserInfo();
    
    /// <summary>
    /// 上传头像
    /// </summary>
    /// <param name="avatar">头像文件</param>
    /// <returns>头像上传结果</returns>
    Task<AvatarUploadResultDto> UploadAvatarAsync(IFormFile avatar);
}