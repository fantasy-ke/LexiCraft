using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Idempotency.Abstractions;
using BuildingBlocks.Idempotency.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Idempotency.Internal;

internal sealed class RedisIdempotencyStore(
    IRedisConnectionFactory connectionFactory,
    IOptions<IdempotencyOptions> options) : IIdempotencyStore
{
    private const string CompleteScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
            redis.call('DEL', KEYS[1])
            return 1
        end
        return 0
        """;

    private const string AbandonScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IdempotencyOptions _options = options.Value;

    public async Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        string fingerprint,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        var database = GetDatabase();
        var leaseKey = BuildKey(key, "lease");
        var resultKey = BuildKey(key, "result");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var completed = await ReadCompletedAsync(database, resultKey, fingerprint, cancellationToken);
            if (completed != null)
                return completed;

            var ownerToken = Guid.NewGuid().ToString("N");
            var leaseValue = BuildLeaseValue(fingerprint, ownerToken);
            var acquired = await database
                .StringSetAsync(leaseKey, leaseValue, processingTimeout, When.NotExists)
                .WaitAsync(cancellationToken);

            if (acquired)
            {
                completed = await ReadCompletedAsync(database, resultKey, fingerprint, cancellationToken);
                if (completed == null)
                {
                    return new IdempotencyAcquireResult(
                        IdempotencyAcquireStatus.Acquired,
                        new IdempotencyLease(key, fingerprint, ownerToken));
                }

                await DeleteLeaseIfOwnedAsync(database, leaseKey, leaseValue, cancellationToken);
                return completed;
            }

            completed = await ReadCompletedAsync(database, resultKey, fingerprint, cancellationToken);
            if (completed != null)
                return completed;

            var currentLease = await database.StringGetAsync(leaseKey).WaitAsync(cancellationToken);
            if (currentLease.IsNull)
                continue;

            var currentFingerprint = ReadLeaseFingerprint(currentLease!);
            return new IdempotencyAcquireResult(
                string.Equals(currentFingerprint, fingerprint, StringComparison.Ordinal)
                    ? IdempotencyAcquireStatus.InProgress
                    : IdempotencyAcquireStatus.FingerprintMismatch);
        }

        throw new InvalidOperationException("Unable to read a stable idempotency lease state.");
    }

    public async Task<bool> CompleteAsync(
        IdempotencyLease lease,
        IdempotencyStoredResponse response,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        var leaseKey = BuildKey(lease.Key, "lease");
        var resultKey = BuildKey(lease.Key, "result");
        var leaseValue = BuildLeaseValue(lease.Fingerprint, lease.OwnerToken);
        var envelope = new StoredResponseEnvelope(
            lease.Fingerprint,
            response.StatusCode,
            response.ContentType,
            response.Body,
            response.Replayable);
        var serialized = JsonSerializer.Serialize(envelope, SerializerOptions);
        var retentionMilliseconds = checked((long)retention.TotalMilliseconds);

        var result = await database
            .ScriptEvaluateAsync(
                CompleteScript,
                [leaseKey, resultKey],
                [leaseValue, serialized, retentionMilliseconds])
            .WaitAsync(cancellationToken);

        return !result.IsNull && (long)result == 1;
    }

    public async Task<bool> AbandonAsync(
        IdempotencyLease lease,
        CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        var leaseKey = BuildKey(lease.Key, "lease");
        var leaseValue = BuildLeaseValue(lease.Fingerprint, lease.OwnerToken);

        return await DeleteLeaseIfOwnedAsync(database, leaseKey, leaseValue, cancellationToken);
    }

    private async Task<IdempotencyAcquireResult?> ReadCompletedAsync(
        IDatabase database,
        RedisKey resultKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var value = await database.StringGetAsync(resultKey).WaitAsync(cancellationToken);
        if (value.IsNull)
            return null;

        var envelope = JsonSerializer.Deserialize<StoredResponseEnvelope>((string)value!, SerializerOptions)
                       ?? throw new InvalidDataException("Stored idempotency response is invalid.");

        if (!string.Equals(envelope.Fingerprint, fingerprint, StringComparison.Ordinal))
            return new IdempotencyAcquireResult(IdempotencyAcquireStatus.FingerprintMismatch);

        return new IdempotencyAcquireResult(
            IdempotencyAcquireStatus.Completed,
            Response: new IdempotencyStoredResponse(
                envelope.StatusCode,
                envelope.ContentType,
                envelope.Body,
                envelope.Replayable));
    }

    private async Task<bool> DeleteLeaseIfOwnedAsync(
        IDatabase database,
        RedisKey leaseKey,
        RedisValue leaseValue,
        CancellationToken cancellationToken)
    {
        var result = await database
            .ScriptEvaluateAsync(AbandonScript, [leaseKey], [leaseValue])
            .WaitAsync(cancellationToken);

        return !result.IsNull && (long)result == 1;
    }

    private IDatabase GetDatabase()
    {
        return string.IsNullOrWhiteSpace(_options.RedisInstanceName)
            ? connectionFactory.GetDatabase()
            : connectionFactory.GetDatabase(_options.RedisInstanceName);
    }

    private RedisKey BuildKey(string key, string suffix)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.Prefix)
            ? "lexicraft:idempotency"
            : _options.Prefix.Trim().TrimEnd(':');
        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return $"{prefix}:{{{keyHash}}}:{suffix}";
    }

    private static string BuildLeaseValue(string fingerprint, string ownerToken)
    {
        return $"{ownerToken}:{fingerprint}";
    }

    private static string ReadLeaseFingerprint(string leaseValue)
    {
        var separatorIndex = leaseValue.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0)
            throw new InvalidDataException("Stored idempotency lease is invalid.");

        return leaseValue[(separatorIndex + 1)..];
    }

    private sealed record StoredResponseEnvelope(
        string Fingerprint,
        int StatusCode,
        string? ContentType,
        byte[] Body,
        bool Replayable);
}
