using System.Collections.Immutable;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     权限定义提供程序基类
/// </summary>
public abstract class PermissionDefinitionProvider
{
    /// <summary>
    ///     设置权限定义
    /// </summary>
    /// <param name="context"></param>
    public abstract void Define(PermissionDefinitionContext context);
}

/// <summary>
///     权限定义上下文
/// </summary>
public class PermissionDefinitionContext
{
    /// <summary>
    ///     所有权限定义（用于快速查找）
    /// </summary>
    private readonly Dictionary<string, PermissionDefinition> _permissions = new();

    private readonly List<PermissionDefinition> _rootPermissions = [];

    /// <summary>
    ///     根权限集合
    /// </summary>
    public ImmutableList<PermissionDefinition> RootPermissions => _rootPermissions.ToImmutableList();

    /// <summary>
    ///     添加根权限
    /// </summary>
    /// <param name="permission"></param>
    public void AddRootPermission(PermissionDefinition permission)
    {
        _rootPermissions.Add(permission);
        _permissions[permission.Name] = permission;
    }

    /// <summary>
    ///     获取指定名称的权限，如果不存在则返回null
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public PermissionDefinition? GetPermissionOrNull(string name)
    {
        if (_permissions.TryGetValue(name, out var permission)) return permission;

        // 在所有权限中查找
        var allPermissions = GetAllPermissions();
        var found = allPermissions.FirstOrDefault(p => p.Name == name);
        if (found != null) _permissions[name] = found;
        return found;
    }

    /// <summary>
    ///     创建权限或获取已存在的权限
    /// </summary>
    /// <param name="name"></param>
    /// <param name="displayName"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public PermissionDefinition CreatePermission(string name, string? displayName, string? description)
    {
        var existingPermission = GetPermissionOrNull(name);
        if (existingPermission != null) return existingPermission;

        var permission = new PermissionDefinition(name, displayName, description);
        AddRootPermission(permission);
        return permission;
    }

    /// <summary>
    ///     获取所有权限定义（扁平化）
    /// </summary>
    /// <returns></returns>
    public IEnumerable<PermissionDefinition> GetAllPermissions()
    {
        return _rootPermissions.Union(
            _rootPermissions.SelectMany(p => p.GetAllChildren()));
    }
}