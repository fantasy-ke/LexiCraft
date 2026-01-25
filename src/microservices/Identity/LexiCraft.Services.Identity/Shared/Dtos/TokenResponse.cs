namespace LexiCraft.Services.Identity.Shared.Dtos;

/// <summary>
///     登录令牌响应
/// </summary>
/// <param name="Token">访问令牌</param>
/// <param name="RefreshToken">刷新令牌</param>
public record TokenResponse(
    string Token,
    string RefreshToken);