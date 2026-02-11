using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain.Internal;
using IdGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.EntityFrameworkCore.Interceptors;

public class AuditableEntityInterceptor(IServiceProvider? serviceProvider = null) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;
        var userContext = serviceProvider?.GetService<IUserContext>();
        var idGenerator = serviceProvider?.GetService<IdGenerator>();

        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    HandleAddedState(entry, userContext, idGenerator);
                    break;
                case EntityState.Modified:
                    HandleModifiedState(entry, userContext);
                    break;
                case EntityState.Deleted:
                    HandleDeletedState(entry, userContext);
                    break;
            }
    }

    private void HandleAddedState(EntityEntry<IEntity> entry, IUserContext? userContext, IdGenerator? idGenerator)
    {
        SetEntityId(entry, idGenerator);

        if (entry.Entity is ICreatable creatableEntity)
            // 只有当CreateAt未被赋值时才设置当前时间
            if (creatableEntity.CreateAt == default)
                creatableEntity.CreateAt = DateTime.Now;

        if (entry.Entity is ISoftDeleted { IsDeleted: false } softDeleted) softDeleted.IsDeleted = false;

        // 处理创建人信息
        ProcessCreatableEntity(entry, userContext);
    }

    private void HandleModifiedState(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is IUpdatable updatableEntity)
            // 设置更新时间为当前时间，无论是否已赋值
            updatableEntity.UpdateAt = DateTime.Now;

        // 处理更新人信息
        ProcessUpdatableEntity(entry, userContext);
    }

    private void HandleDeletedState(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is not ISoftDeleted softDeletedEntity) return;

        entry.Reload();

        // 只有当DeleteAt未被赋值时才设置
        if (softDeletedEntity.DeleteAt == null) softDeletedEntity.DeleteAt = DateTime.Now;

        // 处理删除人信息
        ProcessSoftDeletedEntity(entry, userContext);
    }

    private void SetEntityId(EntityEntry<IEntity> entry, IdGenerator? idGenerator)
    {
        var idProperty = entry.Entity.GetType().GetProperty("Id");
        if (idProperty == null || !idProperty.CanWrite) return;

        var idType = idProperty.PropertyType;
        var idValue = idProperty.GetValue(entry.Entity);

        if (idType == typeof(Guid) || idType == typeof(Guid?))
        {
            if (idValue == null || (Guid)idValue == Guid.Empty)
                idProperty.SetValue(entry.Entity, Guid.NewGuid());
        }
        else if (idType == typeof(long) || idType == typeof(long?))
        {
            if (idValue == null || (long)idValue <= 0)
                idProperty.SetValue(entry.Entity, idGenerator?.CreateId() ?? 0);
        }
        else if (typeof(IStrongId<Guid>).IsAssignableFrom(idType))
        {
            if (idValue == null || ((IStrongId<Guid>)idValue).Value == Guid.Empty)
                idProperty.SetValue(entry.Entity, Activator.CreateInstance(idType, Guid.NewGuid()));
        }
        else if (typeof(IStrongId<long>).IsAssignableFrom(idType))
        {
            if (idValue == null || ((IStrongId<long>)idValue).Value <= 0)
                idProperty.SetValue(entry.Entity, Activator.CreateInstance(idType, idGenerator?.CreateId() ?? 0));
        }
    }

    private void ProcessCreatableEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is ICreatable creatable)
            SetIfNotSetString(() => creatable.CreateByName, v => creatable.CreateByName = v,
                () => userContext?.UserName ?? "systemUser");

        ProcessGenericId(entry, entry.Entity.GetType(), typeof(ICreatable<>), nameof(ICreatable<int>.CreateById),
            userContext?.UserId);
    }

    private void ProcessUpdatableEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is IUpdatable updatable)
            SetIfNotSetString(() => updatable.UpdateByName, v => updatable.UpdateByName = v,
                () => userContext?.UserName ?? "systemUser");

        ProcessGenericId(entry, entry.Entity.GetType(), typeof(IUpdatable<>), nameof(IUpdatable<int>.UpdateById),
            userContext?.UserId);
    }

    private void ProcessSoftDeletedEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is ISoftDeleted softDeleted)
            SetIfNotSetString(() => softDeleted.DeleteByName, v => softDeleted.DeleteByName = v,
                () => userContext?.UserName);

        ProcessGenericId(entry, entry.Entity.GetType(), typeof(ISoftDeleted<>), nameof(ISoftDeleted<int>.DeleteById),
            userContext?.UserId);
    }

    private void ProcessGenericId(EntityEntry<IEntity> entry, Type entityType, Type genericInterfaceDefinition,
        string propertyName, Guid? userId)
    {
        var interfaceType = entityType.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterfaceDefinition);

        if (interfaceType == null) return;

        var property = interfaceType.GetProperty(propertyName);
        if (property == null || !property.CanWrite) return;

        var currentValue = property.GetValue(entry.Entity);
        var targetType = interfaceType.GetGenericArguments()[0];

        // 检查是否已赋值
        var isSet = false;
        if (currentValue != null)
        {
            if (targetType.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(targetType);
                isSet = !currentValue.Equals(defaultValue);
            }
            else
            {
                // 引用类型不为 null 即视为已赋值
                isSet = true;
            }
        }

        if (!isSet)
        {
            var newValue = CreateUserKey(targetType, userId);
            if (newValue != null) property.SetValue(entry.Entity, newValue);
        }
    }

    private object? CreateUserKey(Type targetType, Guid? userId)
    {
        if (userId == null) return null;

        if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            return userId.Value;

        if (typeof(IStrongId<Guid>).IsAssignableFrom(targetType))
            return Activator.CreateInstance(targetType, userId.Value);

        return null;
    }


    /// <summary>
    ///     专门用于字符串类型的SetIfNotSet方法
    /// </summary>
    /// <param name="getter">获取当前字符串值的函数</param>
    /// <param name="setter">设置新字符串值的Action</param>
    /// <param name="valueProvider">提供新字符串值的函数</param>
    private void SetIfNotSetString(Func<string?> getter, Action<string?> setter, Func<string?> valueProvider)
    {
        var currentValue = getter();
        if (!string.IsNullOrEmpty(currentValue)) return;
        var newValue = valueProvider();
        setter(newValue);
    }
}