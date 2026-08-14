using System.Net;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Permissions.Features.AddPermission;

public record AddPermissionCommand(UserId UserId, List<string> Permissions)
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
    IPermissionCache permissionCache,
    IPermissionDefinitionManager permissionDefinitionManager,
    IAuthorizationSynchronization authorizationSynchronization)
    : ICommandHandler<AddPermissionCommand, bool>
{
    public async Task<bool> Handle(AddPermissionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var unknownPermissions = command.Permissions
                .Where(permission => !permissionDefinitionManager.TryGetPermission(permission, out _))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (unknownPermissions.Length > 0)
            {
                ThrowUserFriendlyException.ThrowException(
                    $"Unknown permissions: {string.Join(',', unknownPermissions)}");
                return false;
            }

            return await authorizationSynchronization.ExecuteAsync(
                $"permission:{command.UserId.Value:N}",
                async token =>
                {
                    var user = await userRepository.Query()
                        .Include(u => u.Permissions)
                        .FirstOrDefaultAsync(u => u.Id == command.UserId, token);

                    if (user == null)
                    {
                        ThrowUserFriendlyException.ThrowException("未找到指定用户");
                        return false;
                    }

                    await permissionCache.RemoveUserPermissionsAsync(command.UserId.Value, token);

                    user.AddPermissions(command.Permissions);
                    await userRepository.UpdateAsync(user);
                    await userRepository.SaveChangesAsync();

                    return true;
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw;
        }
        catch (Exception e)
        {
            ThrowUserFriendlyException.ThrowException($"批量添加权限失败：{e.Message}");
            return false;
        }
    }
}
