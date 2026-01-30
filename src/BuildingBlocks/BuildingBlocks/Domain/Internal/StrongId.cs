namespace BuildingBlocks.Domain.Internal;

/// <summary>
///     强类型 ID 标记接口
/// </summary>
public interface IStrongId;

/// <summary>
///     强类型 ID 接口
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IStrongId<out TValue> : IStrongId
{
    TValue Value { get; }
}

/// <summary>
///     强类型 ID 基类
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract record StrongId<TValue>(TValue Value) : IStrongId<TValue>, IComparable<StrongId<TValue>>, IComparable
    where TValue : notnull, IComparable<TValue>, IComparable
{
    public int CompareTo(StrongId<TValue>? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Value.CompareTo(other.Value);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is StrongId<TValue> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(StrongId<TValue>)}");
    }

    public override string ToString() => Value.ToString() ?? string.Empty;
}
