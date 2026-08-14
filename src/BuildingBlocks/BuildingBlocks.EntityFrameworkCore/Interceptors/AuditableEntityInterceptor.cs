using BuildingBlocks.Domain.Internal;
using IdGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BuildingBlocks.Contexts;

namespace BuildingBlocks.EntityFrameworkCore.Interceptors;

public class AuditableEntityInterceptor(
    IUserContext? userContext = null,
    IdGenerator? idGenerator = null) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var utcNow = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    HandleAddedState(entry, utcNow);
                    break;
                case EntityState.Modified:
                    HandleModifiedState(entry, utcNow);
                    break;
                case EntityState.Deleted:
                    HandleDeletedState(entry, utcNow);
                    break;
            }
    }

    private void HandleAddedState(EntityEntry<IEntity> entry, DateTime utcNow)
    {
        SetEntityId(entry);

        if (entry.Entity is ICreatable creatableEntity && creatableEntity.CreateAt == default)
            creatableEntity.CreateAt = utcNow;


        ProcessCreatableEntity(entry);
    }

    private void HandleModifiedState(EntityEntry<IEntity> entry, DateTime utcNow)
    {
        if (entry.Entity is IUpdatable updatableEntity)
            updatableEntity.UpdateAt = utcNow;

        ProcessUpdatableEntity(entry);
    }

    private void HandleDeletedState(EntityEntry<IEntity> entry, DateTime utcNow)
    {
        if (entry.Entity is not ISoftDeleted softDeletedEntity) return;

        entry.State = EntityState.Unchanged;
        softDeletedEntity.IsDeleted = true;
        softDeletedEntity.DeleteAt ??= utcNow;
        ProcessSoftDeletedEntity(entry);

        MarkModified(entry, nameof(ISoftDeleted.IsDeleted));
        MarkModified(entry, nameof(ISoftDeleted.DeleteAt));
        MarkModified(entry, nameof(ISoftDeleted.DeleteByName));
        MarkModified(entry, nameof(ISoftDeleted<int>.DeleteById));
    }

    private static void MarkModified(EntityEntry<IEntity> entry, string propertyName)
    {
        if (entry.Metadata.FindProperty(propertyName) is not null)
            entry.Property(propertyName).IsModified = true;
    }

    private void SetEntityId(EntityEntry<IEntity> entry)
    {
        var idProperty = entry.Entity.GetType().GetProperty("Id");
        if (idProperty == null || !idProperty.CanWrite) return;

        var idType = idProperty.PropertyType;
        var idValue = idProperty.GetValue(entry.Entity);

        if (idType == typeof(Guid) || idType == typeof(Guid?))
        {
            if (idValue == null || (Guid)idValue == Guid.Empty)
                idProperty.SetValue(entry.Entity, Guid.CreateVersion7());
        }
        else if (idType == typeof(long) || idType == typeof(long?))
        {
            if ((idValue == null || (long)idValue <= 0) && idGenerator != null)
                idProperty.SetValue(entry.Entity, idGenerator.CreateId());
        }
        else if (typeof(IStrongId<Guid>).IsAssignableFrom(idType))
        {
            if (idValue == null || ((IStrongId<Guid>)idValue).Value == Guid.Empty)
                idProperty.SetValue(entry.Entity, Activator.CreateInstance(idType, Guid.CreateVersion7()));
        }
        else if (typeof(IStrongId<long>).IsAssignableFrom(idType) && idGenerator != null)
        {
            if (idValue == null || ((IStrongId<long>)idValue).Value <= 0)
                idProperty.SetValue(entry.Entity, Activator.CreateInstance(idType, idGenerator.CreateId()));
        }
    }

    private void ProcessCreatableEntity(EntityEntry<IEntity> entry)
    {
        if (entry.Entity is ICreatable creatable)
            SetIfNotSetString(
                () => creatable.CreateByName,
                value => creatable.CreateByName = value,
                () => userContext?.UserName ?? "systemUser");

        ProcessGenericId(
            entry,
            entry.Entity.GetType(),
            typeof(ICreatable<>),
            nameof(ICreatable<int>.CreateById),
            userContext?.UserId);
    }

    private void ProcessUpdatableEntity(EntityEntry<IEntity> entry)
    {
        if (entry.Entity is IUpdatable updatable)
            SetIfNotSetString(
                () => updatable.UpdateByName,
                value => updatable.UpdateByName = value,
                () => userContext?.UserName ?? "systemUser");

        ProcessGenericId(
            entry,
            entry.Entity.GetType(),
            typeof(IUpdatable<>),
            nameof(IUpdatable<int>.UpdateById),
            userContext?.UserId);
    }

    private void ProcessSoftDeletedEntity(EntityEntry<IEntity> entry)
    {
        if (entry.Entity is ISoftDeleted softDeleted)
            SetIfNotSetString(
                () => softDeleted.DeleteByName,
                value => softDeleted.DeleteByName = value,
                () => userContext?.UserName);

        ProcessGenericId(
            entry,
            entry.Entity.GetType(),
            typeof(ISoftDeleted<>),
            nameof(ISoftDeleted<int>.DeleteById),
            userContext?.UserId);
    }

    private static void ProcessGenericId(
        EntityEntry<IEntity> entry,
        Type entityType,
        Type genericInterfaceDefinition,
        string propertyName,
        Guid? userId)
    {
        var interfaceType = entityType.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterfaceDefinition);

        if (interfaceType == null) return;

        var property = interfaceType.GetProperty(propertyName);
        if (property == null || !property.CanWrite) return;

        var currentValue = property.GetValue(entry.Entity);
        var targetType = interfaceType.GetGenericArguments()[0];
        if (IsSet(currentValue, targetType)) return;

        var newValue = CreateUserKey(targetType, userId);
        if (newValue != null) property.SetValue(entry.Entity, newValue);
    }

    private static bool IsSet(object? currentValue, Type targetType)
    {
        if (currentValue == null) return false;
        if (!targetType.IsValueType) return true;

        var defaultValue = Activator.CreateInstance(targetType);
        return !currentValue.Equals(defaultValue);
    }

    private static object? CreateUserKey(Type targetType, Guid? userId)
    {
        if (userId == null) return null;

        if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            return userId.Value;

        if (typeof(IStrongId<Guid>).IsAssignableFrom(targetType))
            return Activator.CreateInstance(targetType, userId.Value);

        return null;
    }

    private static void SetIfNotSetString(
        Func<string?> getter,
        Action<string?> setter,
        Func<string?> valueProvider)
    {
        if (!string.IsNullOrEmpty(getter())) return;
        setter(valueProvider());
    }
}
