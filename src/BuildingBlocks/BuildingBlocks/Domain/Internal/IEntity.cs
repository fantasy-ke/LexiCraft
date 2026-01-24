namespace BuildingBlocks.Domain.Internal;

public interface IEntity<TKey> : IEntity
{
    public TKey Id { get; set; }
}

public class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; set; }
}

/// <summary>
///     万物实体的接口
/// </summary>
public interface IEntity;

/// <summary>
/// 聚合根接口
/// </summary>
public interface IAggregateRoot : IEntity;
