using BuildingBlocks.Domain;
using LexiCraft.Shared.Models;
using LexiCraft.Services.Identity.Identity.Models;

namespace LexiCraft.Services.Identity.Shared.Contracts;

/// <summary>
///     用户权限仓储接口
/// </summary>
public interface IUserPermissionRepository : IQueryRepository<UserPermission>
{
    /// <summary>
    ///     获取用户的所有权限
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<string>> GetUserPermissionsAsync(UserId userId);
}