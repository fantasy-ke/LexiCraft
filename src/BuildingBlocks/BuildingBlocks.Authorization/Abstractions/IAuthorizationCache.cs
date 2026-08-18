namespace BuildingBlocks.Authentication.Abstractions;

/// <summary>
///     为访问令牌会话和权限快照提供共享分布式缓存。
/// </summary>
public interface IAuthorizationCache
{
    /// <summary>读取指定键的分布式授权数据。</summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">完整缓存键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的值；键不存在时返回 <see langword="null"/>。</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>写入带绝对过期时间的分布式授权数据。</summary>
    /// <typeparam name="T">缓存值类型。</typeparam>
    /// <param name="key">完整缓存键。</param>
    /// <param name="value">要写入的非空值。</param>
    /// <param name="expiration">绝对有效期。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>移除指定键的分布式授权数据。</summary>
    /// <param name="key">完整缓存键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>底层缓存报告的移除结果。</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
///     当前用户会话中访问令牌和刷新令牌的摘要指针。
/// </summary>
/// <param name="AccessTokenHash">当前访问令牌摘要。</param>
/// <param name="RefreshTokenHash">当前刷新令牌摘要。</param>
public sealed record AccessTokenCacheEntry(string AccessTokenHash, string RefreshTokenHash);