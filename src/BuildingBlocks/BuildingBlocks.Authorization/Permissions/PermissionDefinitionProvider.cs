using System.Collections.Immutable;

namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     定义服务可识别权限的扩展点。
/// </summary>
public abstract class PermissionDefinitionProvider
{
    /// <summary>
    ///     向上下文注册根权限及其子权限。
    /// </summary>
    public abstract void Define(PermissionDefinitionContext context);
}

/// <summary>
///     收集权限定义并保证根权限名称唯一。
/// </summary>
public sealed class PermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionDefinition> _permissions = new(StringComparer.Ordinal);
    private readonly List<PermissionDefinition> _rootPermissions = [];

    /// <summary>获取当前已注册根权限的不可变快照。</summary>
    public ImmutableList<PermissionDefinition> RootPermissions => _rootPermissions.ToImmutableList();

    /// <summary>添加根权限；同名但非同一实例时抛出异常。</summary>
    public void AddRootPermission(PermissionDefinition permission)
    {
        if (_permissions.TryGetValue(permission.Name, out var existing))
        {
            if (!ReferenceEquals(existing, permission))
                throw new InvalidOperationException($"Permission {permission.Name} is already registered");
            return;
        }

        _rootPermissions.Add(permission);
        _permissions.Add(permission.Name, permission);
    }

    /// <summary>从全部根权限和后代权限中查找指定名称。</summary>
    public PermissionDefinition? GetPermissionOrNull(string name)
    {
        if (_permissions.TryGetValue(name, out var permission))
            return permission;

        permission = GetAllPermissions()
            .FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        if (permission != null)
            _permissions[name] = permission;

        return permission;
    }

    /// <summary>创建或获取根权限。</summary>
    public PermissionDefinition CreatePermission(string name, string? displayName, string? description)
    {
        var existingPermission = GetPermissionOrNull(name);
        if (existingPermission != null)
            return existingPermission;

        var permission = new PermissionDefinition(name, displayName, description);
        AddRootPermission(permission);
        return permission;
    }

    /// <summary>枚举全部根权限和后代权限。</summary>
    public IEnumerable<PermissionDefinition> GetAllPermissions()
    {
        return _rootPermissions.Concat(_rootPermissions.SelectMany(permission => permission.GetAllChildren()));
    }
}