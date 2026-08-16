namespace BuildingBlocks.Idempotency.Tests;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task EndpointWithoutIdempotencyMetadata_BypassesStore()
    {
        var store = new TestIdempotencyStore();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var endpoint = new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "plain endpoint");
        context.SetEndpoint(endpoint);
        var executed = false;
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(context);

        Assert.True(executed);
        Assert.Empty(store.Keys);
    }

    [Fact]
    public async Task RequiredKeyMissing_ReturnsBadRequestWithoutExecutingEndpoint()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute { RequireKey = true }, key: null);
        var executed = false;
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(executed);
        Assert.Empty(store.Keys);
    }

    [Fact]
    public async Task AcquiredReplayRequest_CompletesAndReturnsEndpointResponse()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status201Created;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync("{\"created\":true}");
            });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("{\"created\":true}", await IdempotencyTestContext.ReadResponseBodyAsync(context));
        Assert.NotNull(store.CompletedResponse);
        Assert.True(store.CompletedResponse!.Replayable);
        Assert.Equal("{\"created\":true}", Encoding.UTF8.GetString(store.CompletedResponse.Body));
        Assert.Empty(store.AbandonedLeases);
    }

    [Fact]
    public async Task CompletedReplayRequest_ReplaysResponseWithoutExecutingEndpoint()
    {
        var store = new TestIdempotencyStore();
        store.Enqueue(new IdempotencyAcquireResult(
            IdempotencyAcquireStatus.Completed,
            Response: new IdempotencyStoredResponse(
                StatusCodes.Status200OK,
                "text/plain",
                Encoding.UTF8.GetBytes("stored"),
                true)));
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var executed = false;
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Equal("true", context.Response.Headers[IdempotencyMiddleware.ReplayedHeaderName].ToString());
        Assert.Equal("stored", await IdempotencyTestContext.ReadResponseBodyAsync(context));
        Assert.False(executed);
    }

    [Fact]
    public async Task CompletedRejectRequest_ReturnsConflict()
    {
        var store = new TestIdempotencyStore();
        store.Enqueue(new IdempotencyAcquireResult(
            IdempotencyAcquireStatus.Completed,
            Response: new IdempotencyStoredResponse(StatusCodes.Status200OK, null, [], false)));
        var context = IdempotencyTestContext.Create(new IdempotentAttribute(IdempotencyMode.Reject));
        var middleware = IdempotencyTestContext.CreateMiddleware(store, _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task FingerprintMismatch_ReturnsConflict()
    {
        var store = new TestIdempotencyStore();
        store.Enqueue(new IdempotencyAcquireResult(IdempotencyAcquireStatus.FingerprintMismatch));
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var middleware = IdempotencyTestContext.CreateMiddleware(store, _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task ReplayRequest_WaitsForCompletedRequestAndReplaysIt()
    {
        var store = new TestIdempotencyStore();
        store.Enqueue(
            new IdempotencyAcquireResult(IdempotencyAcquireStatus.InProgress),
            new IdempotencyAcquireResult(
                IdempotencyAcquireStatus.Completed,
                Response: new IdempotencyStoredResponse(
                    StatusCodes.Status202Accepted,
                    "text/plain",
                    Encoding.UTF8.GetBytes("finished"),
                    true)));
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ => throw new InvalidOperationException("replay should not execute"),
            new IdempotencyOptions
            {
                ReplayWaitTimeout = TimeSpan.FromMilliseconds(100),
                ReplayPollInterval = TimeSpan.FromMilliseconds(1)
            });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("finished", await IdempotencyTestContext.ReadResponseBodyAsync(context));
        Assert.Equal(2, store.Keys.Count);
    }

    [Fact]
    public async Task EndpointFailure_AbandonsLease()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ => throw new InvalidOperationException("endpoint failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Single(store.AbandonedLeases);
        Assert.Null(store.CompletedResponse);
        Assert.Empty(await IdempotencyTestContext.ReadResponseBodyAsync(context));
    }

    [Fact]
    public async Task LockRequest_ReleasesLeaseAfterSuccessfulRequest()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute(IdempotencyMode.Lock));
        var middleware = IdempotencyTestContext.CreateMiddleware(store, _ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Single(store.AbandonedLeases);
        Assert.Null(store.CompletedResponse);
    }

    [Fact]
    public async Task RequestBodyOverLimit_ReturnsPayloadTooLarge()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute(), body: "12345");
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            _ => Task.CompletedTask,
            new IdempotencyOptions { MaxRequestBodyBytes = 4 });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Empty(store.Keys);
    }

    [Fact]
    public async Task ResponseBodyOverLimit_IsMarkedNonReplayableButStillReturned()
    {
        var store = new TestIdempotencyStore();
        var context = IdempotencyTestContext.Create(new IdempotentAttribute());
        var middleware = IdempotencyTestContext.CreateMiddleware(
            store,
            httpContext => httpContext.Response.WriteAsync("12345"),
            new IdempotencyOptions { MaxResponseBodyBytes = 4 });

        await middleware.InvokeAsync(context);

        Assert.Equal("12345", await IdempotencyTestContext.ReadResponseBodyAsync(context));
        Assert.NotNull(store.CompletedResponse);
        Assert.False(store.CompletedResponse!.Replayable);
        Assert.Empty(store.CompletedResponse.Body);
    }

    [Fact]
    public async Task StorageKeyIsScopedByAuthenticatedUser_AndFingerprintIncludesBody()
    {
        var store = new TestIdempotencyStore();
        var middleware = IdempotencyTestContext.CreateMiddleware(store, _ => Task.CompletedTask);

        await middleware.InvokeAsync(IdempotencyTestContext.Create(
            new IdempotentAttribute(),
            body: "first",
            userId: "user-a"));
        await middleware.InvokeAsync(IdempotencyTestContext.Create(
            new IdempotentAttribute(),
            body: "second",
            userId: "user-b"));

        Assert.Equal(2, store.Keys.Distinct().Count());
        Assert.Equal(2, store.Fingerprints.Distinct().Count());
    }
}