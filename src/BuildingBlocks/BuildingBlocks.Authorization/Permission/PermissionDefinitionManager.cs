using System.Collections.Immutable;
using BuildingBlocks.Authentication.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     汇总已注册的权限提供程序，构建不可变权限清单并拒绝重复权限名称。
/// </summary>
public sealed class PermissionDefinitionManager : IPermissionDefinitionManager
{
    private readonly Dictionary<string, PermissionDefinition> _permissionDict;
    private readonly ImmutableList<PermissionDefinition> _permissions;
    private readonly ImmutableList<PermissionDefinition> _rootPermissions;

    /// <summary>
    ///     从依赖注入容器中的全部 <see cref="PermissionDefinitionProvider"/> 构建权限清单。
    /// </summary>
    public PermissionDefinitionManager(IServiceProvider serviceProvider)
    {
        var context = new PermissionDefinitionContext();
        foreach (var provider in serviceProvider.GetServices<PermissionDefinitionProvider>())
            provider.Define(context);

        _rootPermissions = context.RootPermissions;
        _permissions = context.GetAllPermissions().ToImmutableList();

        var duplicateNames = _permissions.GroupBy(permission => permission.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateNames.Length > 0)
            throw new InvalidOperationException($"Duplicate permissions: {string.Join(',', duplicateNames)}");

        _permissionDict = _permissions.ToDictionary(permission => permission.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public ImmutableList<PermissionDefinition> GetRootPermissions() => _rootPermissions;

    /// <inheritdoc />
    public ImmutableList<PermissionDefinition> GetPermissions() => _permissions;

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