using System.Collections.Immutable;
using BuildingBlocks.Authentication.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication.Permission;

public sealed class PermissionDefinitionManager : IPermissionDefinitionManager
{
    private readonly Dictionary<string, PermissionDefinition> _permissionDict;
    private readonly ImmutableList<PermissionDefinition> _permissions;
    private readonly ImmutableList<PermissionDefinition> _rootPermissions;

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

    public ImmutableList<PermissionDefinition> GetRootPermissions() => _rootPermissions;

    public ImmutableList<PermissionDefinition> GetPermissions() => _permissions;

    public PermissionDefinition? GetPermission(string name)
    {
        return _permissionDict.TryGetValue(name, out var permission) ? permission : null;
    }

    public bool TryGetPermission(string name, out PermissionDefinition? permission)
    {
        return _permissionDict.TryGetValue(name, out permission);
    }
}