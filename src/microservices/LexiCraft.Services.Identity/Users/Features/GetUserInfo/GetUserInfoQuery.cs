using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Users.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Users.Features.GetUserInfo;

public record GetUserInfoQuery(Guid UserId) : IQuery<UserInfoDto>;


public class GetUserInfoQueryHandler(IUserRepository userRepository) 
    : IQueryHandler<GetUserInfoQuery, UserInfoDto>
{
    public async Task<UserInfoDto> Handle(GetUserInfoQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.QueryNoTracking<User>()
            .Where(p => p.Id == query.UserId)
            .Select(p => new UserInfoDto
            {
                UserId = p.Id,
                UserName = p.Username,
                Email = p.Email,
                Avatar = p.Avatar,
                Phone = p.Phone,
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return user ?? new UserInfoDto();
    }
}