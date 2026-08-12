namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     Describes a permission node used for registration, display and assignment validation.
/// </summary>
public class PermissionDefinition
{
    public PermissionDefinition(string name, string? displayName, string? description)
    {
        Name = name;
        DisplayName = displayName ?? name;
        Description = description ?? name;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public PermissionDefinition? Parent { get; private set; }

    public List<PermissionDefinition> Children { get; } = [];

    public PermissionDefinition AddChild(PermissionDefinition permission)
    {
        var existing = GetChildOrNull(permission.Name);
        if (existing != null)
            return existing;

        permission.Parent = this;
        Children.Add(permission);
        return permission;
    }

    public PermissionDefinition CreateChildPermission(
        string name,
        string? displayName,
        string? description)
    {
        return GetChildOrNull(name) ?? AddChild(new PermissionDefinition(name, displayName, description));
    }

    public PermissionDefinition? GetChildOrNull(string name)
    {
        return Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.Ordinal));
    }

    public IEnumerable<PermissionDefinition> GetAllChildren()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllChildren())
                yield return descendant;
        }
    }

    public IEnumerable<string> GetAllChildrenNames()
    {
        return GetAllChildren().Select(child => child.Name);
    }

    public IEnumerable<PermissionDefinition> GetAllParents()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    public IEnumerable<string> GetAllParentNames()
    {
        return GetAllParents().Select(parent => parent.Name);
    }
}