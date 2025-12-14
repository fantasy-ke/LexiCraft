using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
/// 权限定义管理器实现
/// </summary>
public class PermissionDefinitionManager : IPermissionDefinitionManager
{
    private readonly ImmutableList<PermissionDefinition> _rootPermissions;
    private readonly ImmutableList<PermissionDefinition> _permissions;
    private readonly Dictionary<string, PermissionDefinition> _permissionDict;

    public PermissionDefinitionManager(IServiceProvider serviceProvider)
    {
        var context = new PermissionDefinitionContext();
        
        // 通过DI容器获取所有权限提供程序并执行定义
        var providers = serviceProvider.GetServices<PermissionDefinitionProvider>();
        foreach (var provider in providers)
        {
            provider.Define(context);
        }

        _rootPermissions = context.RootPermissions;
        _permissions = context.GetAllPermissions().ToImmutableList();
        _permissionDict = _permissions.ToDictionary(p => p.Name, p => p);
    }
    
    /// <inheritdoc />
    public ImmutableList<PermissionDefinition> GetRootPermissions()
    {
        return _rootPermissions;
    }
    
    /// <inheritdoc />
    public ImmutableList<PermissionDefinition> GetPermissions()
    {
        return _permissions;
    }
    
    /// <inheritdoc />
    public PermissionDefinition? GetPermission(string name)
    {
        return _permissionDict.TryGetValue(name, out var permission) ? permission : null;
    }
    
    /// <inheritdoc />
    public bool TryGetPermission(string name, out PermissionDefinition? permission)
    {
        return _permissionDict.TryGetValue(name, out permission);
    }
}