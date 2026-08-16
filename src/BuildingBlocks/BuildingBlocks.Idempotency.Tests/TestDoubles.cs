namespace BuildingBlocks.Idempotency.Tests;

internal sealed class TestIdempotencyStore : IIdempotencyStore
{
    private readonly Queue<IdempotencyAcquireResult> _acquireResults = new();

    public List<string> Keys { get; } = [];
    public List<string> Fingerprints { get; } = [];
    public List<IdempotencyLease> AbandonedLeases { get; } = [];
    public IdempotencyStoredResponse? CompletedResponse { get; private set; }
    public bool CompleteResult { get; set; } = true;

    public void Enqueue(params IdempotencyAcquireResult[] results)
    {
        foreach (var result in results)
            _acquireResults.Enqueue(result);
    }

    public Task<IdempotencyAcquireResult> TryAcquireAsync(
        string key,
        string fingerprint,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default)
    {
        Keys.Add(key);
        Fingerprints.Add(fingerprint);

        if (_acquireResults.TryDequeue(out var result))
            return Task.FromResult(result);

        return Task.FromResult(
            new IdempotencyAcquireResult(
                IdempotencyAcquireStatus.Acquired,
                new IdempotencyLease(key, fingerprint, "test-owner")));
    }

    public Task<bool> CompleteAsync(
        IdempotencyLease lease,
        IdempotencyStoredResponse response,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        CompletedResponse = response;
        return Task.FromResult(CompleteResult);
    }

    public Task<bool> AbandonAsync(
        IdempotencyLease lease,
        CancellationToken cancellationToken = default)
    {
        AbandonedLeases.Add(lease);
        return Task.FromResult(true);
    }
}

internal static class IdempotencyTestContext
{
    public static DefaultHttpContext Create(
        IdempotentAttribute attribute,
        string? key = "key-1",
        string body = "body",
        string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = Encoding.UTF8.GetByteCount(body);
        context.Response.Body = new MemoryStream();

        if (key != null)
            context.Request.Headers["Idempotency-Key"] = key;

        if (userId != null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", userId)], "test"));
        }

        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(attribute),
            "test endpoint"));
        return context;
    }

    public static IdempotencyMiddleware CreateMiddleware(
        TestIdempotencyStore store,
        RequestDelegate next,
        IdempotencyOptions? options = null)
    {
        return new IdempotencyMiddleware(
            next,
            store,
            Microsoft.Extensions.Options.Options.Create(options ?? new IdempotencyOptions()),
            NullLogger<IdempotencyMiddleware>.Instance);
    }

    public static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}