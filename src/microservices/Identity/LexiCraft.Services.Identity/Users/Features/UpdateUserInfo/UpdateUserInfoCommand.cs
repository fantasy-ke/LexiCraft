using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Users.Features.GetUserInfo;

namespace LexiCraft.Services.Identity.Users.Features.UpdateUserInfo;

public record UpdateUserInfoCommand(Guid UserId, string? Username, string? Avatar)
    : ICommand<GetUserInfoResult>;

public class UpdateUserInfoCommandValidator : AbstractValidator<UpdateUserInfoCommand>
{
    public UpdateUserInfoCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("用户ID不能为空");

        When(x => !string.IsNullOrWhiteSpace(x.Username), () =>
        {
            RuleFor(x => x.Username!)
                .MaximumLength(50).WithMessage("用户名长度不能超过50个字符");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Avatar), () =>
        {
            RuleFor(x => x.Avatar!)
                .MaximumLength(500).WithMessage("头像地址长度不能超过500个字符");
        });
    }
}

public class UpdateUserInfoCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateUserInfoCommand, GetUserInfoResult>
{
    public async Task<GetUserInfoResult> Handle(UpdateUserInfoCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(u => u.Id == command.UserId);
        if (user == null)
        {
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        if (!string.IsNullOrWhiteSpace(command.Username) && command.Username != user!.Username)
        {
            var exists = await userRepository.AnyAsync(u => u.Username == command.Username);
            if (exists)
            {
                ThrowUserFriendlyException.ThrowException("用户名已存在");
            }

            user.UpdateUserName(command.Username!);
        }

        if (!string.IsNullOrWhiteSpace(command.Avatar))
        {
            user!.UpdateAvatar(command.Avatar!);
        }

        await userRepository.UpdateAsync(user!);
        await userRepository.SaveChangesAsync();

        return new GetUserInfoResult(
            UserId: user!.Id,
            UserName: user.Username,
            Email: user.Email,
            Phone: user.Phone,
            Avatar: user.Avatar
        );
    }
}

