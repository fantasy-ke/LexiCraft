namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     描述用于注册、展示和授权校验的权限树节点。
/// </summary>
/// <remarks>
///     父子关系只用于组织和展示；授权时按 <see cref="Name"/> 使用 <see cref="StringComparer.Ordinal"/>
///     精确匹配，持有父权限不会自动获得任何子权限，持有子权限也不会自动获得父权限。
/// </remarks>
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
    /// <param name="permission">要挂到当前节点下的权限定义。</param>
    /// <returns>实际挂载或已存在的同名直接子权限。</returns>
    /// <remarks>此操作只建立权限树关系，不产生任何隐式授权继承。</remarks>
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
    /// <param name="name">唯一且区分大小写的权限完整名称。</param>
    /// <param name="displayName">展示名称；为空时使用权限名称。</param>
    /// <param name="description">权限说明；为空时使用权限名称。</param>
    /// <returns>新建或已存在的同名直接子权限。</returns>
    /// <remarks>父子关系只用于分组，调用此方法不会让父权限自动满足新建的子权限。</remarks>
    public PermissionDefinition CreateChildPermission(
        string name,
        string? displayName,
        string? description)
    {
        return GetChildOrNull(name) ?? AddChild(new PermissionDefinition(name, displayName, description));
    }

    /// <summary>按区分大小写的名称查找直接子权限。</summary>
    /// <param name="name">权限完整名称。</param>
    /// <returns>匹配的直接子权限；未找到时返回 <see langword="null"/>。</returns>
    public PermissionDefinition? GetChildOrNull(string name)
    {
        return Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.Ordinal));
    }

    /// <summary>按深度优先顺序枚举全部后代权限。</summary>
    /// <returns>全部后代权限；不包含当前节点。</returns>
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
    /// <returns>全部后代权限的区分大小写名称；不包含当前节点。</returns>
    public IEnumerable<string> GetAllChildrenNames()
    {
        return GetAllChildren().Select(child => child.Name);
    }

    /// <summary>从直接父节点开始向上枚举全部祖先权限。</summary>
    /// <returns>从直接父节点到根节点的权限序列；不包含当前节点。</returns>
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
    /// <returns>从直接父节点到根节点的权限名称；不包含当前节点。</returns>
    public IEnumerable<string> GetAllParentNames()
    {
        return GetAllParents().Select(parent => parent.Name);
    }
}