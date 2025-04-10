namespace LexiCraft.Domain.Internal;

public interface IEntity<TKey>
{
    public TKey Id { get; set; }
}

public class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; set; }
}