using BuildingBlocks.Domain.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.EntityFrameworkCore.Converters;

/// <summary>
///     强类型 ID 的 EF Core 值转换器
/// </summary>
/// <typeparam name="TStrongId"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class StrongIdValueConverter<TStrongId, TValue>() : ValueConverter<TStrongId, TValue>(
    id => id.Value,
    value => CreateInstance(value))
    where TStrongId : StrongId<TValue>
    where TValue : notnull, IComparable<TValue>, IComparable
{
    private static TStrongId CreateInstance(TValue value)
    {
        return (TStrongId)Activator.CreateInstance(typeof(TStrongId), value)!;
    }
}