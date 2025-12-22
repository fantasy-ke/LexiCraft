namespace LexiCraft.Services.Identity.Shared.Dtos;

/// <summary>
/// OAuth令牌信息
/// </summary>
public class OAuthTokenDto
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// 作用域
    /// </summary>
    public string? Scope { get; set; }
}