using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.EntityFrameworkCore.Converters;

/// <summary>在强类型 ID 与其基础值之间执行 EF Core 值转换。</summary>
/// <typeparam name="TStrongId">派生自 <see cref="StrongId{TValue}"/> 的强类型 ID。</typeparam>
/// <typeparam name="TValue">数据库中保存的基础值类型。</typeparam>
/// <remarks>
///     转换器在闭合泛型类型首次初始化时查找并编译一个接收 <typeparamref name="TValue"/> 的公开构造函数，
///     后续实体物化不再逐次使用反射调用构造函数。
/// </remarks>
/// <exception cref="InvalidOperationException"><typeparamref name="TStrongId"/> 没有公开的单参数基础值构造函数时抛出。</exception>
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
