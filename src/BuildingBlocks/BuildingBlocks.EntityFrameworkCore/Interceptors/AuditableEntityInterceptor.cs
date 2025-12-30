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

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
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
        {
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
    }

    private void HandleAddedState(EntityEntry<IEntity> entry, IUserContext? userContext, IdGenerator? idGenerator)
    {
        SetEntityId(entry, idGenerator);

        if (entry.Entity is ICreatable creatableEntity)
        {
            // 只有当CreateAt未被赋值时才设置当前时间
            if (creatableEntity.CreateAt == default)
            {
                creatableEntity.CreateAt = DateTime.Now;
            }
        }

        if (entry.Entity is ISoftDeleted { IsDeleted: false } softDeleted)
        {
            softDeleted.IsDeleted = false;
        }

        // 处理创建人信息
        ProcessCreatableEntity(entry, userContext);
    }

    private void HandleModifiedState(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is IUpdatable updatableEntity)
        {
            // 设置更新时间为当前时间，无论是否已赋值
            updatableEntity.UpdateAt = DateTime.Now;
        }

        // 处理更新人信息
        ProcessUpdatableEntity(entry, userContext);
    }

    private void HandleDeletedState(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        if (entry.Entity is not ISoftDeleted softDeletedEntity) return;
        
        entry.Reload();
        
        // 只有当DeleteAt未被赋值时才设置
        if (softDeletedEntity.DeleteAt == null)
        {
            softDeletedEntity.DeleteAt = DateTime.Now;
        }

        // 处理删除人信息
        ProcessSoftDeletedEntity(entry, userContext);
    }

    private void SetEntityId(EntityEntry<IEntity> entry, IdGenerator? idGenerator)
    {
        switch (entry.Entity)
        {
            case IEntity<Guid?> guidId when guidId.Id == null || guidId.Id == Guid.Empty:
                guidId.Id = Guid.NewGuid();
                break;
            case IEntity<long?> { Id: null or <= 0 } longId:
                longId.Id = idGenerator?.CreateId() ?? 0;
                break;
        }
    }

    private void ProcessCreatableEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        switch (entry.Entity)
        {
            case ICreatable<Guid?> creatable:
                SetIfNotSet(() => creatable.CreateById, v => creatable.CreateById = v, () => userContext?.UserId);
                SetIfNotSetString(() => creatable.CreateByName, v => creatable.CreateByName = v, () => userContext?.UserName ?? "systemUser");
                break;
            case ICreatable<Guid> creatableValue:
                SetIfNotSet(() => creatableValue.CreateById, v => creatableValue.CreateById = v, () => userContext?.UserId ?? Guid.Empty);
                SetIfNotSetString(() => creatableValue.CreateByName, v => creatableValue.CreateByName = v, () => userContext?.UserName);
                break;
        }
    }

    private void ProcessUpdatableEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        switch (entry.Entity)
        {
            case IUpdatable<Guid?> updatable:
                SetIfNotSet(() => updatable.UpdateById, v => updatable.UpdateById = v, () => userContext?.UserId);
                SetIfNotSetString(() => updatable.UpdateByName, v => updatable.UpdateByName = v, () => userContext?.UserName ?? "systemUser");
                break;
            case IUpdatable<Guid> updatableValue:
                SetIfNotSet(() => updatableValue.UpdateById, v => updatableValue.UpdateById = v, () => userContext?.UserId ?? Guid.Empty);
                SetIfNotSetString(() => updatableValue.UpdateByName, v => updatableValue.UpdateByName = v, () => userContext?.UserName);
                break;
        }
    }

    private void ProcessSoftDeletedEntity(EntityEntry<IEntity> entry, IUserContext? userContext)
    {
        switch (entry.Entity)
        {
            case ISoftDeleted<Guid?> softDeleted:
                SetIfNotSet(() => softDeleted.DeleteById, v => softDeleted.DeleteById = v, () => userContext?.UserId);
                SetIfNotSetString(() => softDeleted.DeleteByName, v => softDeleted.DeleteByName = v, () => userContext?.UserName);
                break;
            case ISoftDeleted<Guid> softDeletedValue:
                SetIfNotSet(() => softDeletedValue.DeleteById, v => softDeletedValue.DeleteById = v, () => userContext?.UserId ?? Guid.Empty);
                SetIfNotSetString(() => softDeletedValue.DeleteByName, v => softDeletedValue.DeleteByName = v, () => userContext?.UserName);
                break;
        }
    }

    /// <summary>
    /// 当目标值未设置时，使用提供的值进行设置
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="getter">获取当前值的函数</param>
    /// <param name="setter">设置新值的Action</param>
    /// <param name="valueProvider">提供新值的函数</param>
    private void SetIfNotSet<T>(Func<T> getter, Action<T> setter, Func<T> valueProvider)
    {
        var currentValue = getter();
        bool isNotSet;

        // 对于引用类型或可空值类型，检查是否为null
        if (typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null)
        {
            isNotSet = EqualityComparer<T>.Default.Equals(currentValue, default(T));
        }
        // 对于非可空值类型，检查是否为默认值
        else
        {
            isNotSet = EqualityComparer<T>.Default.Equals(currentValue, default(T));
        }

        if (!isNotSet) return;
        var newValue = valueProvider();
        setter(newValue);
    }
    
    /// <summary>
    /// 专门用于字符串类型的SetIfNotSet方法
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

