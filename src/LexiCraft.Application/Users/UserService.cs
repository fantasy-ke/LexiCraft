using LexiCraft.Application.Contract.User;
using LexiCraft.Application.Contract.User.Dto;
using LexiCraft.Domain;
using LexiCraft.Domain.Users;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZAnalyzers.Core;
using ZAnalyzers.Core.Attribute;

namespace LexiCraft.Application.Users;

[Route("/api/user")]
[Tags("User")]
[Filter(typeof(ResultEndPointFilter))]
public class UserService(IRepository<User> useRepository, IUserContext userContext):FantasyApi,IUserService
{
    [EndpointSummary("获取用户信息")]
    public async Task<UserInfoDto> GetUserInfo()
    {
        var userId = userContext.UserId;

        return await useRepository.QueryNoTracking<User>().Where(p=>p.Id == userId).Select(p=> new UserInfoDto
        {
            UserId = p.Id,
            UserName = p.Username,
            Email = p.Email,
            Avatar =p.Avatar,
            Phone = p.Phone,
        }).FirstOrDefaultAsync();
    }
}