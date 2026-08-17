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
    /// <summary>
    ///     重放响应时附加到响应的头名称，值为 <c>true</c>，便于客户端区分首次响应与重放响应。
    /// </summary>
    public const string ReplayedHeaderName = "Idempotency-Replayed";

    /// <summary>
    ///     解析后的幂等配置，避免每次请求重复读取 <see cref="IOptions{T}"/>。
    /// </summary>
    private readonly IdempotencyOptions _options = options.Value;

    /// <summary>
    ///     中间件主入口：读取幂等键、计算指纹、获取租约并按状态分支处理请求。
    /// </summary>
    /// <remarks>
    ///     不带 <see cref="IdempotentAttribute"/> 元数据或未提供幂等键时直接放行；
    ///     <see cref="IdempotencyAcquireStatus.Acquired"/> 执行后续管道，
    ///     <see cref="IdempotencyAcquireStatus.Completed"/> 重放或拒绝，
    ///     其余状态按策略返回 400/409/413。
    /// </remarks>
    /// <param name="context">当前 HTTP 上下文。</param>
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

    /// <summary>
    ///     在 Replay 模式下轮询等待并发的首个请求完成，以便直接重放其响应。
    /// </summary>
    /// <param name="storageKey">服务端生成的幂等存储键。</param>
    /// <param name="fingerprint">当前请求的指纹。</param>
    /// <param name="policy">已解析的重放等待与租约配置。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>等待结束后获取的幂等状态；超时仍在进行则返回 <see cref="IdempotencyAcquireStatus.InProgress"/>。</returns>
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

    /// <summary>
    ///     执行已获取租约的请求，并在成功时保存响应或释放租约。
    /// </summary>
    /// <remarks>
    ///     Replay/Reject 模式会拦截响应体以便后续重放或记录；
    ///     非 2xx 响应或 Lock 模式会放弃租约，使相同请求可再次执行。
    /// </remarks>
    /// <param name="context">当前 HTTP 上下文。</param>
    /// <param name="mode">重复请求的处理模式。</param>
    /// <param name="policy">已解析的租约与保留配置。</param>
    /// <param name="lease">本次请求持有的执行租约。</param>
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

    /// <summary>
    ///     处理已完成状态：Replay 模式重放响应体，其余模式返回 409 冲突。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文。</param>
    /// <param name="mode">重复请求的处理模式。</param>
    /// <param name="response">已保存的完成响应；可能为 <see langword="null"/>。</param>
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

    /// <summary>
    ///     尝试释放租约，失败仅记录日志而不影响当前响应。
    /// </summary>
    /// <param name="lease">需要释放的租约。</param>
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

    /// <summary>
    ///     从请求头读取并校验幂等键，返回键或校验错误。
    /// </summary>
    /// <param name="headers">请求头集合。</param>
    /// <param name="attribute">端点上的幂等声明，决定是否强制要求键。</param>
    /// <returns>包含已校验幂等键或错误信息的 <see cref="KeyReadResult"/>。</returns>
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

    /// <summary>
    ///     根据租户/用户、请求方法、路径与客户端键生成稳定的存储键。
    /// </summary>
    /// <remarks>存储键经过 SHA-256 哈希，避免将用户标识或密钥明文写入 Redis。</remarks>
    /// <param name="context">当前 HTTP 上下文。</param>
    /// <param name="clientKey">客户端提供的幂等键。</param>
    /// <returns>十六进制哈希形式的存储键。</returns>
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

    /// <summary>
    ///     合并属性覆盖与全局配置，得到本次请求实际生效的租约与等待参数。
    /// </summary>
    /// <param name="attribute">端点上的幂等声明。</param>
    /// <returns>解析后的 <see cref="ResolvedPolicy"/>。</returns>
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

    /// <summary>
    ///     将属性中的秒数解析为 <see cref="TimeSpan"/>，0 表示使用全局配置，负数抛出异常。
    /// </summary>
    /// <param name="seconds">属性提供的秒数。</param>
    /// <param name="fallback">全局默认配置。</param>
    /// <param name="propertyName">属性名称，用于异常信息。</param>
    /// <returns>解析后的时间间隔。</returns>
    private static TimeSpan ResolveTimeSpan(int seconds, TimeSpan fallback, string propertyName)
    {
        if (seconds < 0)
            throw new InvalidOperationException($"{propertyName} 不能为负数。");

        return seconds == 0 ? fallback : TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    ///     将属性中的毫秒数解析为 <see cref="TimeSpan"/>，0 表示使用全局配置，负数抛出异常。
    /// </summary>
    /// <param name="milliseconds">属性提供的毫秒数。</param>
    /// <param name="fallback">全局默认配置。</param>
    /// <param name="propertyName">属性名称，用于异常信息。</param>
    /// <returns>解析后的时间间隔。</returns>
    private static TimeSpan ResolveMilliseconds(int milliseconds, TimeSpan fallback, string propertyName)
    {
        if (milliseconds < 0)
            throw new InvalidOperationException($"{propertyName} 不能为负数。");

        return milliseconds == 0 ? fallback : TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    ///     以 RFC 9457 ProblemDetails 格式写回错误响应。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文。</param>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="title">问题标题。</param>
    /// <param name="detail">问题描述。</param>
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

    /// <summary>
    ///     表示幂等键读取结果：成功时包含键，失败时包含错误信息。
    /// </summary>
    /// <param name="Key">校验通过的幂等键；未提供且非强制时为 <see langword="null"/>。</param>
    /// <param name="Error">校验失败信息；成功时为 <see langword="null"/>。</param>
    private sealed record KeyReadResult(string? Key, string? Error);

    /// <summary>
    ///     合并属性与全局配置后，本次请求实际生效的幂等参数。
    /// </summary>
    /// <param name="Retention">成功记录保留时间。</param>
    /// <param name="ProcessingTimeout">执行租约有效期。</param>
    /// <param name="ReplayWaitTimeout">Replay 模式最大等待时间。</param>
    private sealed record ResolvedPolicy(
        TimeSpan Retention,
        TimeSpan ProcessingTimeout,
        TimeSpan ReplayWaitTimeout);
}