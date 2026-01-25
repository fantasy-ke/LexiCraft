namespace BuildingBlocks.Domain.Internal;

/// <summary>
///     聚合根基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot;