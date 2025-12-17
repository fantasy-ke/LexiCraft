using BuildingBlocks.Filters;
using LexiCraft.AuthServer.Application.Contract.Users;
using LexiCraft.AuthServer.Domain;
using LexiCraft.AuthServer.Domain.Repository;
using Microsoft.AspNetCore.Http;
using ZAnalyzers.Core;

namespace LexiCraft.AuthServer.Application.Users;

/// <summary>
/// 用户权限服务
/// </summary>
[Route("/api/userPermission")]
[Tags("UserPermission")]
[Filter(typeof(ResultEndPointFilter))]
public class PermissionService(
    IUserPermissionRepository userPermissionRepository)
    : ZAnalyzerApi, IPermissionService
{

    /// <summary>
    /// 为用户分配默认权限
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [EndpointSummary("为用户分配默认权限")]
    public async Task AssignDefaultPermissionsAsync(Guid userId)
    {
        var defaultPermissions = RoleConstant.DefaultUserPermissions.Permissions;
        await userPermissionRepository.AddUserPermissionsAsync(userId, defaultPermissions);
    }

    /// <summary>
    /// 获取用户所有权限（包括继承的权限）
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [EndpointSummary("获取用户所有权限")]
    public async Task<HashSet<string>> GetUserAllPermissionsAsync(Guid userId)
    {
        // 获取用户直接权限
        var directPermissions = await userPermissionRepository.GetUserPermissionsAsync(userId);
        return [..directPermissions];
    }
}