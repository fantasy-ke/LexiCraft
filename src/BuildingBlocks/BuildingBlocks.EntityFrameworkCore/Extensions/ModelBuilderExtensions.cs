using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Extensions;

/// <summary>提供 EF Core 模型的强类型 ID 配置扩展。</summary>
public static class ModelBuilderExtensions
{
    /// <summary>为模型中实现 <see cref="IStrongId"/> 的实体属性自动注册值转换器。</summary>
    /// <param name="modelBuilder">已包含待配置实体类型的模型构建器。</param>
    /// <remarks>
    ///     该方法只检查实体 CLR 类型的公开属性，并根据 <see cref="IStrongId{TValue}"/> 的基础值类型创建
    ///     <see cref="StrongIdValueConverter{TStrongId,TValue}"/>。应在实体已加入模型后调用。
    /// </remarks>
    public static void ConfigureStrongIds(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType
                .GetProperties()
                .Where(p => typeof(IStrongId).IsAssignableFrom(p.PropertyType));

            foreach (var property in properties)
            {
                var valueType = property.PropertyType
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStrongId<>))
                    ?.GetGenericArguments()[0];

                if (valueType != null)
                {
                    var converterType =
                        typeof(StrongIdValueConverter<,>).MakeGenericType(property.PropertyType, valueType);
                    var converter = Activator.CreateInstance(converterType);

                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion((dynamic)converter!);
                }
            }
        }
    }
}