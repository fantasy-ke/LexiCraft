namespace BuildingBlocks.Authentication.Contract;

public sealed record AccessTokenValidationResult(bool SessionValid, bool ServiceAvailable)
{
    public static readonly AccessTokenValidationResult Current = new(true, true);

    public static readonly AccessTokenValidationResult InvalidSession = new(false, true);

    public static readonly AccessTokenValidationResult Unavailable = new(true, false);
}

public interface IAccessTokenValidator
{
    Task<AccessTokenValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
