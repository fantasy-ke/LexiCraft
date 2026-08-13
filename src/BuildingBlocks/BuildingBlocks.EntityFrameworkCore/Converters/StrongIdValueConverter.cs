using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.EntityFrameworkCore.Converters;

/// <summary>
///     强类型 ID 的 EF Core 值转换器
/// </summary>
public class StrongIdValueConverter<TStrongId, TValue>() : ValueConverter<TStrongId, TValue>(
    id => id.Value,
    value => Factory(value))
    where TStrongId : StrongId<TValue>
    where TValue : notnull, IComparable<TValue>, IComparable
{
    private static readonly Func<TValue, TStrongId> Factory = CreateFactory();

    private static Func<TValue, TStrongId> CreateFactory()
    {
        var constructor = typeof(TStrongId).GetConstructor([typeof(TValue)])
                          ?? throw new InvalidOperationException(
                              $"{typeof(TStrongId).Name} must expose a constructor accepting {typeof(TValue).Name}.");
        var value = Expression.Parameter(typeof(TValue), "value");
        return Expression.Lambda<Func<TValue, TStrongId>>(Expression.New(constructor, value), value).Compile();
    }
}
