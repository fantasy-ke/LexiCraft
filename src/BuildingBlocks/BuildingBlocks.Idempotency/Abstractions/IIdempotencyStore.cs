namespace BuildingBlocks.Idempotency.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        string fingerprint,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        IdempotencyLease lease,
        IdempotencyStoredResponse response,
        TimeSpan retention,
        CancellationToken cancellationToken = default);

    Task<bool> AbandonAsync(
        IdempotencyLease lease,
        CancellationToken cancellationToken = default);
}

public enum IdempotencyAcquireStatus
{
    Acquired,
    InProgress,
    Completed,
    FingerprintMismatch
}

public sealed record IdempotencyLease(string Key, string Fingerprint, string OwnerToken);

public sealed record IdempotencyStoredResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    bool Replayable);

public sealed record IdempotencyAcquireResult(
    IdempotencyAcquireStatus Status,
    IdempotencyLease? Lease = null,
    IdempotencyStoredResponse? Response = null);
