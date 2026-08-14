namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     缓存 Identity API 使用的用户权限完整快照。
/// </summary>
public interface IPermissionCache
{
    /// <summary>获取完整权限快照；缓存不存在或读取失败时返回 <see langword="null"/>。</summary>
    Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>写入用户的完整权限快照，包括空权限集合。</summary>
    Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>移除用户权限快照，使下一次验证重新读取权威存储。</summary>
    Task RemoveUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);
}