using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Contracts;

namespace LexiCraft.Services.Identity.Permissions.Features.RemovePermission;

public record RemovePermissionCommand(Guid UserId, List<string> Permissions)
    : ICommand<bool>;

public class RemovePermissionCommandValidator : AbstractValidator<RemovePermissionCommand>
{
    public RemovePermissionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空");

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("权限列表不能为空")
            .Must(list => list.Count > 0).WithMessage("权限列表至少包含一个权限");

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage("权限名称不能为空")
            .MaximumLength(200).WithMessage("权限名称长度不能超过200个字符");
    }
}

public class RemovePermissionCommandHandler(
    IUserPermissionRepository userPermissionRepository,
    IPermissionCache permissionCache)
    : ICommandHandler<RemovePermissionCommand, bool>
{
    public async Task<bool> Handle(RemovePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 数据库操作：批量删除权限（一次性删除，避免循环）
            await userPermissionRepository.RemoveUserPermissionsAsync(command.UserId, command.Permissions);

            // 同步更新缓存：批量删除权限
            await permissionCache.RemovePermissionsAsync(command.UserId, command.Permissions);

            return true;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"批量删除权限失败：{e.Message}");
            return false;
        }
    }
}
