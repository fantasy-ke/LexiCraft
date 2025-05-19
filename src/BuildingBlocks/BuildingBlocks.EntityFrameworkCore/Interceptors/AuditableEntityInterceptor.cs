using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain.Internal;
using IdGen;
using Microsoft.EntityFrameworkCore;
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
                {
                    switch (entry.Entity)
                    {
                        case IEntity<Guid?> guidId:
                            if (guidId.Id != null || guidId.Id != Guid.Empty)
                                return;
                            guidId.Id = Guid.NewGuid();
                            break;
                        case IEntity<long?> longId:
                            if (longId.Id != null || longId.Id > 0)
                                return;
                            longId.Id = idGenerator?.CreateId() ?? 0;
                            break;
                    }

                    if (entry.Entity is ICreatable entity)
                    {
                        entity.CreateAt = DateTime.Now;
                    }

                    if (entry.Entity is ISoftDeleted { IsDeleted: false } softDeleted)
                    {
                        softDeleted.IsDeleted = false;
                    }

                    switch (entry.Entity)
                    {
                        case ICreatable<Guid?> creatable:
                            creatable.CreateById = userContext?.UserId ?? null;
                            creatable.CreateByName = userContext?.UserName ?? "systemUser";
                            break;
                        case ICreatable<Guid> creatableValue:
                            creatableValue.CreateById = userContext?.UserId ?? Guid.Empty;
                            creatableValue.CreateByName = userContext?.UserName;
                            break;
                    }

                    break;
                }
                case EntityState.Modified:
                {
                    if (entry.Entity is IUpdatable entity)
                    {
                        entity.UpdateAt = DateTime.Now;
                    }

                    switch (entry.Entity)
                    {
                        case IUpdatable<Guid?> updatable:
                            updatable.UpdateById = userContext?.UserId ?? null;
                            updatable.UpdateByName = userContext?.UserName ?? "systemUser";
                            break;
                        case IUpdatable<Guid> updatableValue:
                            updatableValue.UpdateById = userContext?.UserId ?? Guid.Empty;
                            updatableValue.UpdateByName = userContext?.UserName;
                            break;
                    }

                    break;
                }
                case EntityState.Deleted:
                {
                    if (entry.Entity is not ISoftDeleted)
                    {
                        return;
                    }
                    entry.Reload();
                    if (entry.Entity is ISoftDeleted entity)
                    {
                        entity.DeleteAt = DateTime.Now;
                    }

                    switch (entry.Entity)
                    {
                        case ISoftDeleted<Guid?> updatable:
                            updatable.DeleteById = userContext?.UserId;
                            updatable.DeleteByName = userContext?.UserName;
                            break;
                        case ISoftDeleted<Guid> updatableValue:
                            updatableValue.DeleteById = userContext?.UserId ?? Guid.Empty;
                            updatableValue.DeleteByName = userContext?.UserName;
                            break;
                    }

                    break;
                }
            }
        }
    }
}