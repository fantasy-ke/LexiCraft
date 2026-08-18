namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     缓存 Identity API 使用的用户权限完整快照。
/// </summary>
public interface IPermissionCache
{
    /// <summary>获取完整权限快照；缓存不存在或读取失败时返回 <see langword="null"/>。</summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>区分大小写的完整权限集合；未命中或读取失败时返回 <see langword="null"/>。</returns>
    Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>写入用户的完整权限快照，包括空权限集合。</summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="permissions">按精确名称保存的完整权限集合。</param>
    /// <param name="expiration">可选有效期；为空时由实现选择安全默认值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>移除用户权限快照，使下一次验证重新读取权威存储。</summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RemoveUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);
}