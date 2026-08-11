using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildingBlocks.Messaging.Tests;

public sealed class EventLocalClientTests
{
    [Fact]
    public async Task PublishAsync_ShouldApplyBackpressure_WhenBoundedQueueIsFull()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        services.AddSingleton<EventHandlerInvoker>();
        services.AddSingleton(Options.Create(new EventBusOptions
        {
            EnableLocal = true,
            Local = new LocalEventBusConfig { Capacity = 1 }
        }));

        var handler = new BlockingEventHandler();
        services.AddSingleton(handler);
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        services.AddSingleton<EventLocalClient>();

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<EventLocalClient>();
        var serializer = provider.GetRequiredService<IHandlerSerializer>();
        using var stopSource = new CancellationTokenSource();
        var consumeTask = client.ConsumeStartAsync(stopSource.Token);

        await client.PublishAsync(typeof(TestEvent), serializer.SerializeJson(new TestEvent(1)));
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await client.PublishAsync(typeof(TestEvent), serializer.SerializeJson(new TestEvent(2)));

        try
        {
            using var publishSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.PublishAsync(
                        typeof(TestEvent),
                        serializer.SerializeJson(new TestEvent(3)),
                        publishSource.Token)
                    .AsTask());
        }
        finally
        {
            handler.Release.TrySetResult(true);
            client.Complete();
            await consumeTask.WaitAsync(TimeSpan.FromSeconds(2));
        }

        Assert.Equal(2, handler.HandledCount);
        Assert.Equal(1, handler.MaxConcurrency);
    }

    [Fact]
    public async Task PublishAsync_ShouldFailFast_WhenHandlerRepublishesIntoFullQueue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        services.AddSingleton<EventHandlerInvoker>();
        services.AddSingleton(Options.Create(new EventBusOptions
        {
            EnableLocal = true,
            Local = new LocalEventBusConfig { Capacity = 1 }
        }));

        var handler = new BlockingEventHandler();
        services.AddSingleton(handler);
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        services.AddSingleton<EventLocalClient>();

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<EventLocalClient>();
        var serializer = provider.GetRequiredService<IHandlerSerializer>();
        using var stopSource = new CancellationTokenSource();
        var consumeTask = client.ConsumeStartAsync(stopSource.Token);

        handler.AfterRelease = cancellationToken => client.PublishAsync(
                typeof(TestEvent),
                serializer.SerializeJson(new TestEvent(3)),
                cancellationToken)
            .AsTask();

        try
        {
            await client.PublishAsync(typeof(TestEvent), serializer.SerializeJson(new TestEvent(1)));
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await client.PublishAsync(typeof(TestEvent), serializer.SerializeJson(new TestEvent(2)));
            handler.Release.TrySetResult(true);

            var exception = await handler.ReentrantPublishResult.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsType<EventClientException>(exception);
        }
        finally
        {
            handler.Release.TrySetResult(true);
            client.Complete();
            await consumeTask.WaitAsync(TimeSpan.FromSeconds(2));
        }

        Assert.Equal(2, handler.HandledCount);
    }

    [Fact]
    public async Task Complete_ShouldReleasePublisherWaitingForCapacity()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();
        services.AddSingleton<EventHandlerInvoker>();
        services.AddSingleton(Options.Create(new EventBusOptions
        {
            EnableLocal = true,
            Local = new LocalEventBusConfig { Capacity = 1 }
        }));
        services.AddSingleton<EventLocalClient>();

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<EventLocalClient>();
        var serializer = provider.GetRequiredService<IHandlerSerializer>();

        await client.PublishAsync(typeof(TestEvent), serializer.SerializeJson(new TestEvent(1)));
        var waitingPublisher = client.PublishAsync(
                typeof(TestEvent),
                serializer.SerializeJson(new TestEvent(2)))
            .AsTask();

        Assert.False(waitingPublisher.IsCompleted);
        client.Complete();

        var exception = await Assert.ThrowsAsync<EventClientException>(() => waitingPublisher);
        Assert.IsType<System.Threading.Channels.ChannelClosedException>(exception.InnerException);
    }

    public sealed record TestEvent(int Value);

    private sealed class BlockingEventHandler : IEventHandler<TestEvent>
    {
        private int _concurrency;
        private int _handledCount;
        private int _maxConcurrency;

        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Func<CancellationToken, Task>? AfterRelease { get; set; }

        public TaskCompletionSource<Exception?> ReentrantPublishResult { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int HandledCount => Volatile.Read(ref _handledCount);
        public int MaxConcurrency => Volatile.Read(ref _maxConcurrency);

        public async Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            var concurrency = Interlocked.Increment(ref _concurrency);
            UpdateMaxConcurrency(concurrency);

            try
            {
                if (Interlocked.Increment(ref _handledCount) == 1)
                {
                    Started.TrySetResult(true);
                    await Release.Task.WaitAsync(cancellationToken);

                    if (AfterRelease is { } afterRelease)
                    {
                        try
                        {
                            await afterRelease(cancellationToken);
                            ReentrantPublishResult.TrySetResult(null);
                        }
                        catch (Exception ex)
                        {
                            ReentrantPublishResult.TrySetResult(ex);
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _concurrency);
            }
        }

        private void UpdateMaxConcurrency(int concurrency)
        {
            var current = Volatile.Read(ref _maxConcurrency);
            while (concurrency > current)
            {
                var observed = Interlocked.CompareExchange(ref _maxConcurrency, concurrency, current);
                if (observed == current) return;
                current = observed;
            }
        }
    }
}
