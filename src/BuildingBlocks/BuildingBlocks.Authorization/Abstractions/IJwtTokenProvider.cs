namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     生成 Identity 登录流程使用的 JWT 访问令牌和高熵刷新令牌。
/// </summary>
/// <remarks>
///     实现从 <c>OAuthOptions</c> 读取签发方、受众、有效期和签名密钥。调用方不得记录、缓存明文刷新令牌，
///     持久化会话时应仅保存令牌摘要。
/// </remarks>
public interface IJwtTokenProvider
{
    /// <summary>
    ///     为指定用户生成带角色和附加声明的 JWT 访问令牌。
    /// </summary>
    /// <param name="dist">要写入令牌的附加声明名称和值；名称应与平台 Claim 契约一致。</param>
    /// <param name="userId">写入 <see cref="System.Security.Claims.ClaimTypes.Sid"/> 的用户标识。</param>
    /// <param name="roles">写入角色 Claim 的角色名称；角色只用于既有角色策略和管理员旁路，不替代权限精确匹配。</param>
    /// <returns>使用当前 OAuth 配置签名的紧凑 JWT 字符串。</returns>
    string GenerateAccessToken(Dictionary<string, string> dist, Guid userId, string[] roles);

    /// <summary>
    ///     使用密码学安全随机数生成刷新令牌。
    /// </summary>
    /// <returns>Base64 编码的高熵刷新令牌；调用方应只持久化其摘要。</returns>
    string GenerateRefreshToken();
}