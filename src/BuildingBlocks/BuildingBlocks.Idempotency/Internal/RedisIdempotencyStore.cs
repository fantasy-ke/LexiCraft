using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Idempotency.Abstractions;
using BuildingBlocks.Idempotency.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Idempotency.Internal;

/// <summary>
///     基于 Redis 的 <see cref="IIdempotencyStore"/> 实现，利用原子脚本保证租约所有权。
/// </summary>
/// <remarks>
///     租约与完成结果分别存储，完成/放弃通过 Lua 脚本原子校验 OwnerToken，
///     避免并发请求相互覆盖。
/// </remarks>
internal sealed class RedisIdempotencyStore(
    IRedisConnectionFactory connectionFactory,
    IOptions<IdempotencyOptions> options) : IIdempotencyStore
{
    /// <summary>
    ///     完成脚本：仅当租约值仍为当前请求时，写入完成结果并删除租约。
    /// </summary>
    private const string CompleteScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
            redis.call('DEL', KEYS[1])
            return 1
        end
        return 0
        """;

    /// <summary>
    ///     放弃脚本：仅当租约值仍为当前请求时删除租约。
    /// </summary>
    private const string AbandonScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    /// <summary>
    ///     存储响应信封使用的 JSON 序列化选项（Web 默认值）。
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IdempotencyOptions _options = options.Value;
    private readonly string _currentPrefix = NormalizePrefix(options.Value.Prefix);
    private readonly string[] _legacyPrefixes = options.Value.LegacyPrefixes
        .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
        .Select(NormalizePrefix)
        .Where(prefix => !string.Equals(prefix, NormalizePrefix(options.Value.Prefix), StringComparison.Ordinal))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    ///     尝试获取租约或读取已完成响应，最多重试 3 次以容忍并发竞争。
    /// </summary>
    /// <param name="key">服务端生成的幂等存储键。</param>
    /// <param name="fingerprint">请求指纹，用于检测重复键但内容不同的冲突。</param>
    /// <param name="processingTimeout">租约有效期。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>对应状态的 <see cref="IdempotencyAcquireResult"/>。</returns>
    public async Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        string fingerprint,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        var database = GetDatabase();
        var leaseKey = BuildKey(_currentPrefix, key, "lease");
        var resultKey = BuildKey(_currentPrefix, key, "result");
        var legacyKeys = _legacyPrefixes
            .Select(prefix => new KeyPair(
                BuildKey(prefix, key, "lease"),
                BuildKey(prefix, key, "result")))
            .ToArray();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var completed = await ReadCompletedFromAnyPrefixAsync(
                database, resultKey, legacyKeys, fingerprint, cancellationToken);
            if (completed != null)
                return completed;

            var legacyLease = await ReadLegacyLeaseAsync(
                database, legacyKeys, fingerprint, cancellationToken);
            if (legacyLease != null)
                return legacyLease;

            var ownerToken = Guid.NewGuid().ToString("N");
            var leaseValue = BuildLeaseValue(fingerprint, ownerToken);
            var acquired = await database
                .StringSetAsync(leaseKey, leaseValue, processingTimeout, When.NotExists)
                .WaitAsync(cancellationToken);

            if (acquired)
            {
                completed = await ReadCompletedFromAnyPrefixAsync(
                    database, resultKey, legacyKeys, fingerprint, cancellationToken);
                var legacyLeaseAfterAcquire = completed == null
                    ? await ReadLegacyLeaseAsync(database, legacyKeys, fingerprint, cancellationToken)
                    : null;
                if (completed == null && legacyLeaseAfterAcquire == null)
                {
                    return new IdempotencyAcquireResult(
                        IdempotencyAcquireStatus.Acquired,
                        new IdempotencyLease(key, fingerprint, ownerToken));
                }

                await DeleteLeaseIfOwnedAsync(database, leaseKey, leaseValue, cancellationToken);
                return completed ?? legacyLeaseAfterAcquire!;
            }

            completed = await ReadCompletedFromAnyPrefixAsync(
                database, resultKey, legacyKeys, fingerprint, cancellationToken);
            if (completed != null)
                return completed;

            var legacyLeaseAfterFailedAcquire = await ReadLegacyLeaseAsync(
                database, legacyKeys, fingerprint, cancellationToken);
            if (legacyLeaseAfterFailedAcquire != null)
                return legacyLeaseAfterFailedAcquire;

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

    /// <summary>
    ///     原子保存完成响应并释放租约。
    /// </summary>
    /// <param name="lease">当前请求持有的租约。</param>
    /// <param name="response">需要保存的响应。</param>
    /// <param name="retention">完成结果保留时间。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>租约仍属于当前请求且写入成功时返回 <see langword="true"/>。</returns>
    public async Task<bool> CompleteAsync(
        IdempotencyLease lease,
        IdempotencyStoredResponse response,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        var leaseKey = BuildKey(_currentPrefix, lease.Key, "lease");
        var resultKey = BuildKey(_currentPrefix, lease.Key, "result");
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

    /// <summary>
    ///     在租约仍属于当前请求时释放租约。
    /// </summary>
    /// <param name="lease">需要释放的租约。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>租约仍属于当前请求且已释放时返回 <see langword="true"/>。</returns>
    public async Task<bool> AbandonAsync(
        IdempotencyLease lease,
        CancellationToken cancellationToken = default)
    {
        var database = GetDatabase();
        var leaseKey = BuildKey(_currentPrefix, lease.Key, "lease");
        var leaseValue = BuildLeaseValue(lease.Fingerprint, lease.OwnerToken);

        return await DeleteLeaseIfOwnedAsync(database, leaseKey, leaseValue, cancellationToken);
    }

    private async Task<IdempotencyAcquireResult?> ReadCompletedFromAnyPrefixAsync(
        IDatabase database,
        RedisKey currentResultKey,
        IReadOnlyList<KeyPair> legacyKeys,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var completed = await ReadCompletedAsync(
            database, currentResultKey, fingerprint, cancellationToken);
        if (completed != null)
            return completed;

        foreach (var legacyKey in legacyKeys)
        {
            completed = await ReadCompletedAsync(
                database, legacyKey.ResultKey, fingerprint, cancellationToken);
            if (completed != null)
                return completed;
        }

        return null;
    }

    private static async Task<IdempotencyAcquireResult?> ReadLegacyLeaseAsync(
        IDatabase database,
        IReadOnlyList<KeyPair> legacyKeys,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        foreach (var legacyKey in legacyKeys)
        {
            var leaseValue = await database
                .StringGetAsync(legacyKey.LeaseKey)
                .WaitAsync(cancellationToken);
            if (leaseValue.IsNull)
                continue;

            return new IdempotencyAcquireResult(
                string.Equals(ReadLeaseFingerprint(leaseValue!), fingerprint, StringComparison.Ordinal)
                    ? IdempotencyAcquireStatus.InProgress
                    : IdempotencyAcquireStatus.FingerprintMismatch);
        }

        return null;
    }

    /// <summary>
    ///     读取已完成响应，并在指纹不匹配时返回冲突状态。
    /// </summary>
    /// <param name="database">Redis 数据库。</param>
    /// <param name="resultKey">完成结果键。</param>
    /// <param name="fingerprint">当前请求指纹。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已完成结果；不存在或租约已被占用时返回 <see langword="null"/>。</returns>
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

    /// <summary>
    ///     仅在租约值匹配当前请求时删除租约。
    /// </summary>
    /// <param name="database">Redis 数据库。</param>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="leaseValue">期望的租约值（含 OwnerToken）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>删除成功返回 <see langword="true"/>。</returns>
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

    /// <summary>
    ///     根据配置获取目标 Redis 数据库，支持命名实例。
    /// </summary>
    /// <returns>Redis 数据库实例。</returns>
    private IDatabase GetDatabase()
    {
        return string.IsNullOrWhiteSpace(_options.RedisInstanceName)
            ? connectionFactory.GetDatabase()
            : connectionFactory.GetDatabase(_options.RedisInstanceName);
    }

    /// <summary>
    ///     构建带前缀与哈希的 Redis 键，使用哈希标签保证租约与结果落在同一槽。
    /// </summary>
    /// <param name="key">服务端生成的幂等存储键。</param>
    /// <param name="suffix">键用途后缀（lease/result）。</param>
    /// <returns>完整的 Redis 键。</returns>
    private static RedisKey BuildKey(string prefix, string key, string suffix)
    {
        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return $"{prefix}:{{{keyHash}}}:{suffix}";
    }

    private static string NormalizePrefix(string prefix)
    {
        return prefix.Trim().TrimEnd(':');
    }

    /// <summary>
    ///     将 OwnerToken 与指纹编码为租约值，用于所有权校验。
    /// </summary>
    /// <param name="fingerprint">请求指纹。</param>
    /// <param name="ownerToken">随机所有权令牌。</param>
    /// <returns>形如 <c>ownerToken:fingerprint</c> 的租约值。</returns>
    private static string BuildLeaseValue(string fingerprint, string ownerToken)
    {
        return $"{ownerToken}:{fingerprint}";
    }

    /// <summary>
    ///     从租约值中解析出请求指纹。
    /// </summary>
    /// <param name="leaseValue">租约值。</param>
    /// <returns>请求指纹。</returns>
    private static string ReadLeaseFingerprint(string leaseValue)
    {
        var separatorIndex = leaseValue.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0)
            throw new InvalidDataException("Stored idempotency lease is invalid.");

        return leaseValue[(separatorIndex + 1)..];
    }

    private readonly record struct KeyPair(RedisKey LeaseKey, RedisKey ResultKey);

    /// <summary>
    ///     完成响应在 Redis 中的存储信封，附加指纹以支持冲突检测。
    /// </summary>
    /// <param name="Fingerprint">请求指纹。</param>
    /// <param name="StatusCode">响应状态码。</param>
    /// <param name="ContentType">响应内容类型。</param>
    /// <param name="Body">响应体字节。</param>
    /// <param name="Replayable">是否可重放。</param>
    private sealed record StoredResponseEnvelope(
        string Fingerprint,
        int StatusCode,
        string? ContentType,
        byte[] Body,
        bool Replayable);
}
