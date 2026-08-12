using System.Collections.Immutable;

namespace BuildingBlocks.Authentication.Permission;

public abstract class PermissionDefinitionProvider
{
    public abstract void Define(PermissionDefinitionContext context);
}

public sealed class PermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionDefinition> _permissions = new(StringComparer.Ordinal);
    private readonly List<PermissionDefinition> _rootPermissions = [];

    public ImmutableList<PermissionDefinition> RootPermissions => _rootPermissions.ToImmutableList();

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

    public PermissionDefinition CreatePermission(string name, string? displayName, string? description)
    {
        var existingPermission = GetPermissionOrNull(name);
        if (existingPermission != null)
            return existingPermission;

        var permission = new PermissionDefinition(name, displayName, description);
        AddRootPermission(permission);
        return permission;
    }

    public IEnumerable<PermissionDefinition> GetAllPermissions()
    {
        return _rootPermissions.Concat(_rootPermissions.SelectMany(permission => permission.GetAllChildren()));
    }
}