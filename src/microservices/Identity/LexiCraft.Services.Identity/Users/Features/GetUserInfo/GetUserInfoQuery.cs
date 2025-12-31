using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Users.Features.GetUserInfo;

public record GetUserInfoQuery(Guid UserId) : IQuery<GetUserInfoResult>;

public record GetUserInfoResult(
    Guid UserId,
    string UserName,
    string Email,
    string? Phone,
    string Avatar
);


public class GetUserInfoQueryHandler(IUserRepository userRepository) 
    : IQueryHandler<GetUserInfoQuery, GetUserInfoResult>
{
    public async Task<GetUserInfoResult> Handle(GetUserInfoQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.QueryNoTracking<User>()
            .Where(p => p.Id == query.UserId)
            .Select(p => new GetUserInfoResult(
                UserId: p.Id,
                UserName: p.Username,
                Email: p.Email,
                Phone: p.Phone,
                Avatar: p.Avatar
            ))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return user ?? new GetUserInfoResult(
            UserId: Guid.Empty,
            UserName: string.Empty,
            Email: string.Empty,
            Phone: null,
            Avatar: string.Empty
        );
    }
}