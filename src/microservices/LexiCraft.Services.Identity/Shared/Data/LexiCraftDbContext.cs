using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexiCraft.Services.Identity.Shared.Data;

public class LexiCraftDbContext(DbContextOptions options,IServiceProvider serviceProvider = null): DbContext(options)
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<UserSetting> UserSettings { get; set; }
    
    public DbSet<UserOAuth> UserOAuths { get; set; }
    
    public DbSet<LoginLog> LoginLogs { get; set; }
    
    public DbSet<UserPermission> UserPermissions { get; set; }

    private ContextOption ContextOption { get; } = 
        serviceProvider?.GetService<IOptionsSnapshot<ContextOption>>()!.Value;
    
    
    protected virtual bool IsSoftDeleteFilterEnabled
        => ContextOption is { EnableSoftDelete: true };
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //软删除查询过滤
        OnModelCreatingConfigureGlobalFilters(modelBuilder);
    }
    protected virtual void OnModelCreatingConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        var methodInfo = GetType().GetMethod(nameof(ConfigureGlobalFilters), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            methodInfo!.MakeGenericMethod(entityType.ClrType).Invoke(this, [
                modelBuilder, entityType
            ]);
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
    protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
        where TEntity : class
    {
        Expression<Func<TEntity, bool>> expression = null;

        if (typeof(ISoftDeleted).IsAssignableFrom(typeof(TEntity)))
        {
            expression = entity => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(entity, nameof(ISoftDeleted.IsDeleted));
        }
        return expression;
    }

}