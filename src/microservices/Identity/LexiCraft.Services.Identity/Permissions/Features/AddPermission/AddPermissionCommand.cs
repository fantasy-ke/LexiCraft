using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Permissions.Features.AddPermission;

public record AddPermissionCommand(Guid UserId, List<string> Permissions)
    : ICommand<bool>;

public class AddPermissionCommandValidator : AbstractValidator<AddPermissionCommand>
{
    public AddPermissionCommandValidator()
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

public class AddPermissionCommandHandler(
    IUserRepository userRepository,
    IPermissionCache permissionCache)
    : ICommandHandler<AddPermissionCommand, bool>
{
    public async Task<bool> Handle(AddPermissionCommand command, CancellationToken cancellationToken)
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

            // 在领域模型中追加权限
            user.AddPermissions(command.Permissions);

            // 通过聚合根仓储级联持久化
            await userRepository.UpdateAsync(user);
            await userRepository.SaveChangesAsync();

            // 同步更新缓存：批量添加权限
            await permissionCache.AddPermissionsAsync(command.UserId, command.Permissions);

            return true;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"批量添加权限失败：{e.Message}");
            return false;
        }
    }
}