namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     Serializes authorization cache changes across Identity.Api instances.
/// </summary>
public interface IAuthorizationSynchronization
{
    Task<TResult> ExecuteAsync<TResult>(
        string resource,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default);
}