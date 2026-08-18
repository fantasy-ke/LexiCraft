using System.Collections.Immutable;
using BuildingBlocks.Authentication.Permissions;

namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     提供启动时构建的、区分大小写的权限定义只读清单。
/// </summary>
/// <remarks>
///     权限树只表达管理界面的组织关系。授权检查始终按完整权限名精确匹配，授予父权限不会隐式授予任何子权限。
/// </remarks>
public interface IPermissionDefinitionManager
{
    /// <summary>
    ///     获取根权限定义的不可变列表。
    /// </summary>
    /// <returns>全部根权限定义。</returns>
    ImmutableList<PermissionDefinition> GetRootPermissions();

    /// <summary>
    ///     获取根节点及其全部后代权限的不可变列表。
    /// </summary>
    /// <returns>全部已注册权限定义。</returns>
    ImmutableList<PermissionDefinition> GetPermissions();

    /// <summary>
    ///     按区分大小写的完整名称获取权限定义。
    /// </summary>
    /// <param name="name">权限完整名称。</param>
    /// <returns>匹配的权限定义；名称未注册时返回 <see langword="null"/>。</returns>
    PermissionDefinition? GetPermission(string name);

    /// <summary>
    ///     尝试按区分大小写的完整名称获取权限定义。
    /// </summary>
    /// <param name="name">权限完整名称。</param>
    /// <param name="permission">找到的权限定义；未找到时为 <see langword="null"/>。</param>
    /// <returns>名称已注册时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    bool TryGetPermission(string name, out PermissionDefinition? permission);
}