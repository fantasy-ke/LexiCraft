namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     表示当前访问令牌会话的验证结果。
/// </summary>
/// <param name="SessionValid">当前令牌是否仍对应用户的有效会话。</param>
/// <param name="ServiceAvailable">会话验证依赖是否可用。</param>
public sealed record AccessTokenValidationResult(bool SessionValid, bool ServiceAvailable)
{
    /// <summary>会话有效且验证服务可用。</summary>
    public static readonly AccessTokenValidationResult Current = new(true, true);

    /// <summary>验证服务可用，但当前令牌已不是有效会话。</summary>
    public static readonly AccessTokenValidationResult InvalidSession = new(false, true);

    /// <summary>会话验证服务不可用，不能把结果误判为无权限。</summary>
    public static readonly AccessTokenValidationResult Unavailable = new(true, false);
}

/// <summary>
///     验证当前请求中的访问令牌是否仍属于有效登录会话。
/// </summary>
public interface IAccessTokenValidator
{
    /// <summary>
    ///     验证当前访问令牌会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>会话有效性和验证服务可用性。</returns>
    Task<AccessTokenValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
