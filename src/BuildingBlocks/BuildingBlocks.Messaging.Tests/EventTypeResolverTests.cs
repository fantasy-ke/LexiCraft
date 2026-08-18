using BuildingBlocks.MassTransit.EventSourcing.Store;
using Xunit;

namespace BuildingBlocks.Messaging.Tests;

public sealed class EventTypeResolverTests
{
    private static readonly string CurrentAssemblySuffix =
        RemoveFirstSegment(typeof(EventTypeResolverTests).Assembly.GetName().Name!);

    [Fact]
    public void Resolve_ShouldUseAssemblyQualifiedName_WhenTypeStillExists()
    {
        var resolver = new EventTypeResolver([typeof(MigratedEvent).Assembly]);

        var resolution = resolver.Resolve(typeof(MigratedEvent).AssemblyQualifiedName!);

        Assert.Equal(typeof(MigratedEvent), resolution.Type);
        Assert.False(resolution.IsAmbiguous);
    }

    [Fact]
    public void Resolve_ShouldMatchUniqueType_WhenOnlyBrandSegmentChanged()
    {
        var resolver = new EventTypeResolver([typeof(MigratedEvent).Assembly]);
        var storedName = BuildStoredName(RemoveFirstSegment(typeof(MigratedEvent).FullName!));

        var resolution = resolver.Resolve(storedName);

        Assert.Equal(typeof(MigratedEvent), resolution.Type);
        Assert.False(resolution.IsAmbiguous);
    }

    [Fact]
    public void Resolve_ShouldRejectAmbiguousCompatibilityMatch()
    {
        var resolver = new EventTypeResolver([typeof(MigratedEvent).Assembly]);
        var storedName = BuildStoredName(
            RemoveFirstSegment(typeof(FirstBrand.Contracts.DuplicatedEvent).FullName!));

        var resolution = resolver.Resolve(storedName);

        Assert.Null(resolution.Type);
        Assert.True(resolution.IsAmbiguous);
    }

    [Fact]
    public void Resolve_ShouldReturnUnknown_WhenNoRegisteredTypeMatches()
    {
        var resolver = new EventTypeResolver([typeof(MigratedEvent).Assembly]);

        var resolution = resolver.Resolve(BuildStoredName("Contracts.RemovedEvent"));

        Assert.Null(resolution.Type);
        Assert.False(resolution.IsAmbiguous);
    }

    /// <summary>
    ///     构造一个“首段品牌不同、其余部分与当前程序集一致”的历史类型名。
    /// </summary>
    private static string BuildStoredName(string typeNameSuffix)
    {
        return $"Previous.{typeNameSuffix}, Previous.{CurrentAssemblySuffix}";
    }

    private static string RemoveFirstSegment(string value)
    {
        return value[(value.IndexOf('.', StringComparison.Ordinal) + 1)..];
    }

    private sealed record MigratedEvent(Guid Id);
}
