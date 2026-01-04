using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Contracts;

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
    IUserPermissionRepository userPermissionRepository,
    IPermissionCacheService permissionCacheService)
    : ICommandHandler<AddPermissionCommand, bool>
{
    public async Task<bool> Handle(AddPermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 数据库操作：批量添加权限
            await userPermissionRepository.AddUserPermissionsAsync(command.UserId, command.Permissions);

            // 同步更新缓存：批量添加权限（一次性写入，避免循环）
            await permissionCacheService.AddPermissionsAsync(command.UserId, command.Permissions);

            return true;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"批量添加权限失败：{e.Message}");
            return false;
        }
    }
}
