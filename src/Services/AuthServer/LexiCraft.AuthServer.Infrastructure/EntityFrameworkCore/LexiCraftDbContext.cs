using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Shared;
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