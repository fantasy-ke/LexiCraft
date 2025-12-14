using BuildingBlocks.Authentication;
using BuildingBlocks.Domain;
using LexiCraft.AuthServer.Application.Contract.User;
using LexiCraft.AuthServer.Application.Contract.Users;
using LexiCraft.AuthServer.Application.Contract.Users.Authorization;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Domain.Users;
using ZAnalyzers.Core;

namespace LexiCraft.AuthServer.Application.Users;

/// <summary>
/// 用户权限服务
/// </summary>
public class PermissionService(
    IUserPermissionRepository userPermissionRepository)
    : FantasyService, IPermissionService
{

    /// <summary>
    /// 为用户分配默认权限
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task AssignDefaultPermissionsAsync(Guid userId)
    {
        var defaultPermissions = new[]
        {
            "Pages",
            "Pages.Verification",
            "Pages.Verification.Create"
        };

        await userPermissionRepository.AddUserPermissionsAsync(userId, defaultPermissions);
    }

    /// <summary>
    /// 获取用户所有权限（包括继承的权限）
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<HashSet<string>> GetUserAllPermissionsAsync(Guid userId)
    {
        // 获取用户直接权限
        var directPermissions = await userPermissionRepository.GetUserPermissionsAsync(userId);
        var allPermissions = new HashSet<string>(directPermissions);

        // TODO: 添加继承的权限逻辑（如果需要）

        return allPermissions;
    }
}