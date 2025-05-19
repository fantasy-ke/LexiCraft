#nullable enable
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Shared;
using IdGen;
using LexiCraft.AuthServer.Domain.Files;
using LexiCraft.AuthServer.Domain.LoginLogs;
using LexiCraft.AuthServer.Domain.Users;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore;

public class LexiCraftDbContext(DbContextOptions options,IServiceProvider? serviceProvider = null): DbContext(options)
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<UserSetting> UserSettings { get; set; }
    
    public DbSet<UserOAuth> UserOAuths { get; set; }
    
    public DbSet<LoginLog> LoginLogs { get; set; }
    
    public DbSet<FileInfos> FileInfos { get; set; }

    private ContextOption? ContextOption { get; } = 
        serviceProvider?.GetService<IOptionsSnapshot<ContextOption>>()!.Value;
    
    
    protected virtual bool IsSoftDeleteFilterEnabled
        => ContextOption is { EnableSoftDelete: true };
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureAuth();
        
        //软删除查询过滤
        OnModelCreatingConfigureGlobalFilters(modelBuilder);
    }
    
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // 忽略未应用的模型更改警告，这个是.net9出现的新警告，不忽略会影响迁移记录应用
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }


    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BeforeSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new())
    {
        BeforeSaveChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BeforeSaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        var userContext = serviceProvider?.GetService<IUserContext>();
        var idGenerator = serviceProvider?.GetService<IdGenerator>();
        foreach (var entry in entries)
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
    
    protected virtual void OnModelCreatingConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        var methodInfo = GetType().GetMethod(nameof(ConfigureGlobalFilters), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            methodInfo!.MakeGenericMethod(entityType.ClrType).Invoke(this, new object?[]
            {
                modelBuilder, entityType
            });
        }
    }

    /// <summary>
    /// Filters
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="modelBuilder"></param>
    /// <param name="mutableEntityType"></param>

    protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.BaseType != null) return;
        var filterExpression = CreateFilterExpression<TEntity>();
        if (filterExpression != null)
            modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
    }

    /// <summary>
    /// 过滤Expression 软删除
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    protected virtual Expression<Func<TEntity, bool>>? CreateFilterExpression<TEntity>()
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? expression = null;

        if (typeof(ISoftDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            expression = entity => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(entity, nameof(ISoftDeleted.IsDeleted));
        }
        return expression;
    }

}