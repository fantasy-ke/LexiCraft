namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     为 Identity API 提供用户当前生效的权威权限集合。
/// </summary>
public interface IUserPermissionStore
{
    /// <summary>
    ///     获取指定用户当前生效的完整权限集合。
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>区分大小写的权限名称集合。</returns>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);
}