using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Contracts;

namespace LexiCraft.Services.Identity.Permissions.Features.UpdatePermissions;

public record UpdatePermissionsCommand(Guid UserId, List<string> Permissions)
    : ICommand<bool>;

public class UpdatePermissionsCommandValidator : AbstractValidator<UpdatePermissionsCommand>
{
    public UpdatePermissionsCommandValidator()
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

public class UpdatePermissionsCommandHandler(
    IUserPermissionRepository userPermissionRepository,
    IPermissionCacheService permissionCacheService)
    : ICommandHandler<UpdatePermissionsCommand, bool>
{
    public async Task<bool> Handle(UpdatePermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 数据库操作：先删除用户所有权限，再批量新增
            await userPermissionRepository.RemoveAllUserPermissionsAsync(command.UserId);
            await userPermissionRepository.AddUserPermissionsAsync(command.UserId, command.Permissions);

            // 同步更新缓存：直接设置完整的权限集合
            await permissionCacheService.SetUserPermissionsAsync(
                command.UserId,
                command.Permissions.ToHashSet()
            );

            return true;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"更新权限失败：{e.Message}");
            return false;
        }
    }
}
