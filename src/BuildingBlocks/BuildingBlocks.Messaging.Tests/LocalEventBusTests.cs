using System.Threading.Channels;
using BuildingBlocks.MassTransit.LocalEvents;
using BuildingBlocks.MassTransit.Options;
using BuildingBlocks.Mediator;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildingBlocks.Messaging.Tests;

public sealed class LocalEventBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldApplyBackpressure_WhenBoundedQueueIsFull()
    {
        var eventBus = CreateEventBus(capacity: 1);
        await eventBus.PublishAsync(new TestDomainEvent(1));

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            eventBus.PublishAsync(new TestDomainEvent(2), cancellationSource.Token).AsTask());
    }

    [Fact]
    public async Task PublishAsync_ShouldFailFast_WhenConsumerRepublishesIntoFullQueue()
    {
        var eventBus = CreateEventBus(capacity: 1);
        await eventBus.PublishAsync(new TestDomainEvent(1));

        using var consumerScope = eventBus.EnterConsumerScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            eventBus.PublishAsync(new TestDomainEvent(2)).AsTask());
    }

    [Fact]
    public async Task Complete_ShouldReleasePublisherWaitingForCapacity()
    {
        var eventBus = CreateEventBus(capacity: 1);
        await eventBus.PublishAsync(new TestDomainEvent(1));
        var waitingPublisher = eventBus.PublishAsync(new TestDomainEvent(2)).AsTask();

        Assert.False(waitingPublisher.IsCompleted);
        eventBus.Complete();

        await Assert.ThrowsAsync<ChannelClosedException>(() => waitingPublisher);
    }

    [Fact]
    public void OptionalBackends_ShouldBeDisabledByDefault()
    {
        var options = new MassTransitOptions();

        Assert.False(options.Enabled);
        Assert.False(options.Saga.Enabled);
        Assert.False(options.EventSourcing.Enabled);
    }

    private static LocalEventBus CreateEventBus(int capacity)
    {
        return new LocalEventBus(Options.Create(new MassTransitOptions
        {
            LocalEvents = new LocalEventOptions { Capacity = capacity }
        }));
    }

    private sealed record TestDomainEvent(int Value) : IDomainEvent;
}
