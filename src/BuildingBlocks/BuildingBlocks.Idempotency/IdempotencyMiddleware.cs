using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Idempotency.Abstractions;
using BuildingBlocks.Idempotency.Internal;
using BuildingBlocks.Idempotency.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BuildingBlocks.Idempotency;

/// <summary>
///     对带有 <see cref="IdempotentAttribute"/> 元数据的端点执行幂等控制。
/// </summary>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IIdempotencyStore store,
    IOptions<IdempotencyOptions> options,
    ILogger<IdempotencyMiddleware> logger)
{
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    private readonly IdempotencyOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var attribute = context.GetEndpoint()?.Metadata.GetMetadata<IdempotentAttribute>();
        if (attribute == null)
        {
            await next(context);
            return;
        }

        var keyResult = ReadIdempotencyKey(context.Request.Headers, attribute);
        if (keyResult.Error != null)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "无效的幂等键", keyResult.Error);
            return;
        }

        if (keyResult.Key == null)
        {
            await next(context);
            return;
        }

        var policy = ResolvePolicy(attribute);
        var fingerprint = await IdempotencyRequestFingerprint.CreateAsync(
            context.Request,
            _options.MaxRequestBodyBytes,
            context.RequestAborted);
        if (fingerprint == null)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "请求体过大",
                $"幂等请求体不能超过 {_options.MaxRequestBodyBytes} 字节。");
            return;
        }

        var storageKey = BuildStorageKey(context, keyResult.Key);
        var acquireResult = await store.TryAcquireAsync(
            storageKey,
            fingerprint,
            policy.ProcessingTimeout,
            context.RequestAborted);

        if (acquireResult.Status == IdempotencyAcquireStatus.InProgress &&
            attribute.Mode == IdempotencyMode.Replay)
        {
            acquireResult = await WaitForReplayAsync(
                storageKey,
                fingerprint,
                policy,
                context.RequestAborted);
        }

        switch (acquireResult.Status)
        {
            case IdempotencyAcquireStatus.Acquired:
                await ExecuteAcquiredRequestAsync(context, attribute.Mode, policy, acquireResult.Lease!);
                return;
            case IdempotencyAcquireStatus.Completed:
                await HandleCompletedAsync(context, attribute.Mode, acquireResult.Response);
                return;
            case IdempotencyAcquireStatus.FingerprintMismatch:
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    "幂等键冲突",
                    "同一幂等键已用于不同的请求内容。");
                return;
            case IdempotencyAcquireStatus.InProgress:
                context.Response.Headers.RetryAfter = "1";
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    "请求正在处理中",
                    "同一幂等请求仍在处理中，请稍后重试。");
                return;
            default:
                throw new InvalidOperationException($"未知的幂等获取状态: {acquireResult.Status}");
        }
    }

    private async Task<IdempotencyAcquireResult> WaitForReplayAsync(
        string storageKey,
        string fingerprint,
        ResolvedPolicy policy,
        CancellationToken cancellationToken)
    {
        if (policy.ReplayWaitTimeout == TimeSpan.Zero)
            return new IdempotencyAcquireResult(IdempotencyAcquireStatus.InProgress);

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < policy.ReplayWaitTimeout)
        {
            var remaining = policy.ReplayWaitTimeout - stopwatch.Elapsed;
            var delay = remaining < _options.ReplayPollInterval
                ? remaining
                : _options.ReplayPollInterval;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            var result = await store.TryAcquireAsync(
                storageKey,
                fingerprint,
                policy.ProcessingTimeout,
                cancellationToken);
            if (result.Status != IdempotencyAcquireStatus.InProgress)
                return result;
        }

        return new IdempotencyAcquireResult(IdempotencyAcquireStatus.InProgress);
    }

    private async Task ExecuteAcquiredRequestAsync(
        HttpContext context,
        IdempotencyMode mode,
        ResolvedPolicy policy,
        IdempotencyLease lease)
    {
        var originalBody = context.Response.Body;
        BoundedResponseBufferStream? responseBuffer = null;
        var commitResponse = false;

        if (mode is IdempotencyMode.Replay or IdempotencyMode.Reject)
        {
            responseBuffer = new BoundedResponseBufferStream(originalBody, _options.MaxResponseBodyBytes);
            context.Response.Body = responseBuffer;
        }

        try
        {
            try
            {
                await next(context);
                commitResponse = true;
            }
            catch
            {
                await TryAbandonAsync(lease);
                throw;
            }

            if (context.Response.StatusCode is < StatusCodes.Status200OK or >= StatusCodes.Status300MultipleChoices)
            {
                await TryAbandonAsync(lease);
            }
            else if (mode == IdempotencyMode.Lock)
            {
                await TryAbandonAsync(lease);
            }
            else
            {
                var response = mode == IdempotencyMode.Replay
                    ? new IdempotencyStoredResponse(
                        context.Response.StatusCode,
                        context.Response.ContentType,
                        responseBuffer!.GetCapturedBody(),
                        responseBuffer.Replayable)
                    : new IdempotencyStoredResponse(
                        context.Response.StatusCode,
                        context.Response.ContentType,
                        [],
                        false);

                try
                {
                    var completed = await store.CompleteAsync(
                        lease,
                        response,
                        policy.Retention,
                        CancellationToken.None);
                    if (!completed)
                    {
                        logger.LogWarning(
                            "幂等请求完成记录未写入，租约可能已过期或被替换: {Key}",
                            lease.Key);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "幂等请求完成记录写入失败: {Key}", lease.Key);
                }
            }
        }
        finally
        {
            if (responseBuffer != null)
            {
                context.Response.Body = originalBody;
                try
                {
                    if (commitResponse)
                        await responseBuffer.CommitAsync(CancellationToken.None);
                }
                finally
                {
                    await responseBuffer.DisposeAsync();
                }
            }
        }
    }

    private async Task HandleCompletedAsync(
        HttpContext context,
        IdempotencyMode mode,
        IdempotencyStoredResponse? response)
    {
        if (mode != IdempotencyMode.Replay || response is not { Replayable: true })
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "重复请求",
                "该幂等请求已经成功处理，当前策略不允许再次执行或重放响应。");
            return;
        }

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        context.Response.ContentLength = response.Body.Length;
        context.Response.Headers[ReplayedHeaderName] = "true";
        await context.Response.Body.WriteAsync(response.Body, context.RequestAborted);
    }

    private async Task TryAbandonAsync(IdempotencyLease lease)
    {
        try
        {
            var abandoned = await store.AbandonAsync(lease, CancellationToken.None);
            if (!abandoned)
                logger.LogDebug("幂等租约已过期或不再属于当前请求: {Key}", lease.Key);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "释放幂等租约失败: {Key}", lease.Key);
        }
    }

    private KeyReadResult ReadIdempotencyKey(IHeaderDictionary headers, IdempotentAttribute attribute)
    {
        if (!headers.TryGetValue(_options.HeaderName, out var values) || StringValues.IsNullOrEmpty(values))
        {
            return attribute.RequireKey
                ? new KeyReadResult(null, $"必须提供 {_options.HeaderName} 请求头。")
                : new KeyReadResult(null, null);
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
            return new KeyReadResult(null, $"{_options.HeaderName} 必须是单个非空值。");

        var key = values[0]!.Trim();
        if (key.Length > _options.MaxKeyLength)
        {
            return new KeyReadResult(
                null,
                $"{_options.HeaderName} 不能超过 {_options.MaxKeyLength} 个字符。");
        }

        return new KeyReadResult(key, null);
    }

    private static string BuildStorageKey(HttpContext context, string clientKey)
    {
        var userScope = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.User.FindFirstValue("sub")
                        ?? "anonymous";
        var material = string.Join(
            '\n',
            userScope,
            context.Request.Method,
            context.Request.PathBase.Value,
            context.Request.Path.Value,
            clientKey);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private ResolvedPolicy ResolvePolicy(IdempotentAttribute attribute)
    {
        return new ResolvedPolicy(
            ResolveTimeSpan(attribute.RetentionSeconds, _options.Retention, nameof(attribute.RetentionSeconds)),
            ResolveTimeSpan(
                attribute.ProcessingTimeoutSeconds,
                _options.ProcessingTimeout,
                nameof(attribute.ProcessingTimeoutSeconds)),
            ResolveMilliseconds(
                attribute.ReplayWaitTimeoutMilliseconds,
                _options.ReplayWaitTimeout,
                nameof(attribute.ReplayWaitTimeoutMilliseconds)));
    }

    private static TimeSpan ResolveTimeSpan(int seconds, TimeSpan fallback, string propertyName)
    {
        if (seconds < 0)
            throw new InvalidOperationException($"{propertyName} 不能为负数。");

        return seconds == 0 ? fallback : TimeSpan.FromSeconds(seconds);
    }

    private static TimeSpan ResolveMilliseconds(int milliseconds, TimeSpan fallback, string propertyName)
    {
        if (milliseconds < 0)
            throw new InvalidOperationException($"{propertyName} 不能为负数。");

        return milliseconds == 0 ? fallback : TimeSpan.FromMilliseconds(milliseconds);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            },
            cancellationToken: context.RequestAborted);
    }

    private sealed record KeyReadResult(string? Key, string? Error);

    private sealed record ResolvedPolicy(
        TimeSpan Retention,
        TimeSpan ProcessingTimeout,
        TimeSpan ReplayWaitTimeout);
}