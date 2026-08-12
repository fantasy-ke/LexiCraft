namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     描述用于注册、展示和授权校验的权限树节点。
/// </summary>
public class PermissionDefinition
{
    /// <summary>
    ///     创建权限定义。
    /// </summary>
    /// <param name="name">唯一且区分大小写的权限名称。</param>
    /// <param name="displayName">展示名称；为空时使用权限名称。</param>
    /// <param name="description">权限说明；为空时使用权限名称。</param>
    public PermissionDefinition(string name, string? displayName, string? description)
    {
        Name = name;
        DisplayName = displayName ?? name;
        Description = description ?? name;
    }

    /// <summary>获取唯一权限名称。</summary>
    public string Name { get; }

    /// <summary>获取面向管理界面的展示名称。</summary>
    public string DisplayName { get; }

    /// <summary>获取权限用途说明。</summary>
    public string Description { get; }

    /// <summary>获取父权限；根权限为 <see langword="null"/>。</summary>
    public PermissionDefinition? Parent { get; private set; }

    /// <summary>获取直接子权限集合。</summary>
    public List<PermissionDefinition> Children { get; } = [];

    /// <summary>
    ///     添加直接子权限；同名子权限已存在时返回原定义。
    /// </summary>
    public PermissionDefinition AddChild(PermissionDefinition permission)
    {
        var existing = GetChildOrNull(permission.Name);
        if (existing != null)
            return existing;

        permission.Parent = this;
        Children.Add(permission);
        return permission;
    }

    /// <summary>创建或获取指定名称的直接子权限。</summary>
    public PermissionDefinition CreateChildPermission(
        string name,
        string? displayName,
        string? description)
    {
        return GetChildOrNull(name) ?? AddChild(new PermissionDefinition(name, displayName, description));
    }

    /// <summary>按区分大小写的名称查找直接子权限。</summary>
    public PermissionDefinition? GetChildOrNull(string name)
    {
        return Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.Ordinal));
    }

    /// <summary>按深度优先顺序枚举全部后代权限。</summary>
    public IEnumerable<PermissionDefinition> GetAllChildren()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllChildren())
                yield return descendant;
        }
    }

    /// <summary>枚举全部后代权限名称。</summary>
    public IEnumerable<string> GetAllChildrenNames()
    {
        return GetAllChildren().Select(child => child.Name);
    }

    /// <summary>从直接父节点开始向上枚举全部祖先权限。</summary>
    public IEnumerable<PermissionDefinition> GetAllParents()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>枚举全部祖先权限名称。</summary>
    public IEnumerable<string> GetAllParentNames()
    {
        return GetAllParents().Select(parent => parent.Name);
    }
}