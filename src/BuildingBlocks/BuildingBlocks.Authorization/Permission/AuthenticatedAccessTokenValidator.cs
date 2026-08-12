using BuildingBlocks.Authentication.Contract;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     Business services validate the JWT locally and delegate current-session validation to Identity.Api.
/// </summary>
public sealed class AuthenticatedAccessTokenValidator : IAccessTokenValidator
{
    public Task<AccessTokenValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessTokenValidationResult.Current);
    }
}
