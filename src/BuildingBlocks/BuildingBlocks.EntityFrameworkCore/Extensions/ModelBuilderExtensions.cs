using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Extensions;

public static class ModelBuilderExtensions
{
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