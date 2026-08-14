using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.EntityFrameworkCore.Repositories;
using BuildingBlocks.EntityFrameworkCore.Transactions;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection WithDbAccess<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TDbContext : DbContext
    {
        services.TryAddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<TDbContext>((serviceProvider, options) =>
        {
            optionsAction(options);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });
        services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();

        return services;
    }

    public static IServiceCollection WithRepository<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        return services.TryAddRepository<TDbContext>([typeof(TDbContext).Assembly]);
    }

    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
        where TDbContext : DbContext
    {
        var entityTypes = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsEntity());

        foreach (var entityType in entityTypes)
        {
            var queryRepositoryInterfaceType = typeof(IQueryRepository<>).MakeGenericType(entityType);
            var queryRepositoryImplementationType =
                typeof(QueryRepository<,>).MakeGenericType(typeof(TDbContext), entityType);
            services.TryAddScoped(queryRepositoryInterfaceType, queryRepositoryImplementationType);

            if (!typeof(IAggregateRoot).IsAssignableFrom(entityType)) continue;

            var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(entityType);
            services.TryAddDefaultRepository(
                repositoryInterfaceType,
                typeof(Repository<,>).MakeGenericType(typeof(TDbContext), entityType));
        }

        return services;
    }

    private static bool IsEntity(this Type type)
    {
        return type is { IsClass: true, IsGenericType: false, IsAbstract: false } &&
               typeof(IEntity).IsAssignableFrom(type);
    }

    private static void TryAddDefaultRepository(
        this IServiceCollection services,
        Type repositoryInterfaceType,
        Type repositoryImplementationType)
    {
        if (repositoryInterfaceType.IsAssignableFrom(repositoryImplementationType))
            services.TryAddScoped(repositoryInterfaceType, repositoryImplementationType);
    }
}
