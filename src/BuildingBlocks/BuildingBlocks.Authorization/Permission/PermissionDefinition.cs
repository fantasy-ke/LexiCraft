namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     权限定义
/// </summary>
public class PermissionDefinition
{
    public PermissionDefinition(string name, string? displayName, string? description)
    {
        Name = name;
        DisplayName = displayName ?? name;
        Description = description ?? name;
    }

    /// <summary>
    ///     权限名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    ///     描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     父权限（用于构建权限层次结构）
    /// </summary>
    public PermissionDefinition? Parent { get; set; }

    /// <summary>
    ///     子权限列表
    /// </summary>
    public List<PermissionDefinition> Children { get; } = new();

    /// <summary>
    ///     添加子权限
    /// </summary>
    /// <param name="permission"></param>
    /// <returns></returns>
    public PermissionDefinition AddChild(PermissionDefinition permission)
    {
        permission.Parent = this;
        Children.Add(permission);
        return permission;
    }

    /// <summary>
    ///     创建子权限
    /// </summary>
    /// <param name="name"></param>
    /// <param name="displayName"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public PermissionDefinition CreateChildPermission(string name, string? displayName, string? description)
    {
        var child = new PermissionDefinition(name, displayName, description)
        {
            Parent = this
        };
        Children.Add(child);
        return child;
    }

    /// <summary>
    ///     获取指定名称的子权限
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public PermissionDefinition? GetChildOrNull(string name)
    {
        return Children.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    ///     获取所有后代权限（递归获取所有子权限）
    /// </summary>
    /// <returns></returns>
    public IEnumerable<PermissionDefinition> GetAllChildren()
    {
        foreach (var child in Children)
        {
            yield return child;

            foreach (var descendant in child.GetAllChildren()) yield return descendant;
        }
    }

    /// <summary>
    ///     获取所有后代权限名称
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetAllChildrenNames()
    {
        return GetAllChildren().Select(c => c.Name);
    }

    /// <summary>
    ///     获取所有祖先权限（递归获取所有父权限）
    /// </summary>
    /// <returns></returns>
    public IEnumerable<PermissionDefinition> GetAllParents()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    ///     获取所有祖先权限名称
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetAllParentNames()
    {
        return GetAllParents().Select(p => p.Name);
    }
}