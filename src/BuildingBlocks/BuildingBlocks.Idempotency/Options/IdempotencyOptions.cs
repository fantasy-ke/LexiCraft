namespace BuildingBlocks.Idempotency.Options;

/// <summary>
///     幂等处理中间件配置。
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public string HeaderName { get; set; } = "Idempotency-Key";
    public string Prefix { get; set; } = "lexicraft:idempotency";
    public string? RedisInstanceName { get; set; }
    public TimeSpan Retention { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan ReplayWaitTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan ReplayPollInterval { get; set; } = TimeSpan.FromMilliseconds(50);
    public long MaxRequestBodyBytes { get; set; } = 1024 * 1024;
    public long MaxResponseBodyBytes { get; set; } = 1024 * 1024;
    public int MaxKeyLength { get; set; } = 200;
}
