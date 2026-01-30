using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Shared;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexiCraft.Services.Vocabulary.Shared.Data;

public class VocabularyDbContext(
    DbContextOptions<VocabularyDbContext> options,
    IServiceProvider? serviceProvider = null) : DbContext(options)
{
    public DbSet<Word> Words { get; set; }
    public DbSet<WordList> WordLists { get; set; }
    public DbSet<WordListItem> WordListItems { get; set; }
    public DbSet<UserWordState> UserWordStates { get; set; }

    private ContextOption ContextOption { get; } =
        serviceProvider?.GetService<IOptionsSnapshot<ContextOption>>()?.Value ??
        new ContextOption { EnableSoftDelete = true };

    protected virtual bool IsSoftDeleteFilterEnabled => ContextOption is { EnableSoftDelete: true };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VocabularyDbContext).Assembly);
        modelBuilder.ConfigureStrongIds();

        // 软删除查询过滤
        OnModelCreatingConfigureGlobalFilters(modelBuilder);
    }

    protected virtual void OnModelCreatingConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        var methodInfo = GetType()
            .GetMethod(nameof(ConfigureGlobalFilters), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(ISoftDeleted).IsAssignableFrom(entityType.ClrType))
                methodInfo!.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder, entityType]);
    }

    protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder,
        IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.BaseType != null) return;
        var filterExpression = CreateFilterExpression<TEntity>();
        if (filterExpression != null)
            modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
    }

    protected virtual Expression<Func<TEntity, bool>>? CreateFilterExpression<TEntity>()
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? expression = null;

        if (typeof(ISoftDeleted).IsAssignableFrom(typeof(TEntity)))
            expression = entity =>
                !IsSoftDeleteFilterEnabled || !EF.Property<bool>(entity, nameof(ISoftDeleted.IsDeleted));
        return expression;
    }
}