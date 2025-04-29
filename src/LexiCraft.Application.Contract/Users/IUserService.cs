using LexiCraft.Application.Contract.User.Dto;

namespace LexiCraft.Application.Contract.User;

public interface IUserService
{
    /// <summary>
    /// 当前登录用户信息
    /// </summary>
    /// <returns></returns>
    Task<UserInfoDto> GetUserInfo();
}