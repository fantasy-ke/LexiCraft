using BuildingBlocks.Domain.Internal;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Identity.Models;

public class UserOAuth : SimpleAuditEntity<Guid>
{
    private UserOAuth()
    {
        UserId = UserId.Empty;
    } // For EF Core

    public UserOAuth(UserId userId, string provider, string providerUserId, string accessToken,
        DateTimeOffset accessTokenExpiresAt, string refreshToken = "")
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        RefreshToken = refreshToken;
    }

    /// <summary>
    ///     获取或设置用户的唯一标识符
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    ///     获取或设置 OAuth 提供者的名称（如 Google, Facebook, etc.）
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    ///     获取或设置 OAuth 提供者分配给用户的唯一标识符
    /// </summary>
    public string ProviderUserId { get; private set; } = string.Empty;

    /// <summary>
    ///     获取或设置用户的访问令牌
    /// </summary>
    public string AccessToken { get; private set; } = string.Empty;

    /// <summary>
    ///     获取或设置访问令牌的过期时间
    /// </summary>
    public DateTimeOffset AccessTokenExpiresAt { get; private set; }

    /// <summary>
    ///     获取或设置刷新令牌（如果适用）
    /// </summary>
    public string RefreshToken { get; private set; } = string.Empty;

    public void UpdateTokens(string accessToken, DateTimeOffset accessTokenExpiresAt, string refreshToken)
    {
        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        if (!string.IsNullOrEmpty(refreshToken)) RefreshToken = refreshToken;
    }
}