using BuildingBlocks.Authentication.Abstractions;

namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     验证当前用户会话是否满足指定权限集合。
/// </summary>
public interface IPermissionCheck
{
    /// <summary>
    ///     验证全部指定权限；空集合仅用于确认会话可继续授权。
    /// </summary>
    /// <param name="permissionNames">需要同时满足的权限名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>授权、会话和依赖服务状态。</returns>
    Task<PermissionValidationResult> CheckAsync(IReadOnlyCollection<string> permissionNames,
        CancellationToken cancellationToken = default);
}
