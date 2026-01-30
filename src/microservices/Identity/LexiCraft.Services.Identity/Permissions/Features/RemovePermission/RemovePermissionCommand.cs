using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Shared.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Permissions.Features.RemovePermission;

public record RemovePermissionCommand(UserId UserId, List<string> Permissions)
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
    IUserRepository userRepository,
    IPermissionCache permissionCache)
    : ICommandHandler<RemovePermissionCommand, bool>
{
    public async Task<bool> Handle(RemovePermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 通过聚合根操作：加载包含权限的用户实体
            var user = await userRepository.Query()
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user == null)
            {
                ThrowUserFriendlyException.ThrowException("未找到指定用户");
                return false;
            }

            // 在领域模型中移除权限
            user.RemovePermissions(command.Permissions);

            // 通过聚合根仓储级联持久化
            await userRepository.UpdateAsync(user);
            await userRepository.SaveChangesAsync();

            // 同步更新缓存：批量删除权限
            await permissionCache.RemovePermissionsAsync(command.UserId.Value, command.Permissions);

            return true;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"批量删除权限失败：{e.Message}");
            return false;
        }
    }
}