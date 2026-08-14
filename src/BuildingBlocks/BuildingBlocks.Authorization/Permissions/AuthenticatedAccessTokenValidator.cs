using BuildingBlocks.Authentication.Abstractions;

namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     业务服务的占位会话验证器：JWT 由本地认证中间件校验，当前会话由后续 Identity API 调用统一确认。
/// </summary>
internal sealed class AuthenticatedAccessTokenValidator : IAccessTokenValidator
{
    /// <inheritdoc />
    public Task<AccessTokenValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessTokenValidationResult.Current);
    }
}
