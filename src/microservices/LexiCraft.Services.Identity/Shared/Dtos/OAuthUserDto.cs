namespace LexiCraft.Services.Identity.Shared.Dtos;

/// <summary>
/// OAuth用户信息
/// </summary>
public class OAuthUserDto
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? AvatarUrl { get; set; }
}