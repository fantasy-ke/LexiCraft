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

/// <summary>提供 EF Core 上下文、仓储和工作单元的依赖注入注册扩展。</summary>
public static class DependencyInjectionExtensions
{
    /// <summary>注册数据库上下文、作用域审计拦截器和 EF Core 工作单元。</summary>
    /// <typeparam name="TDbContext">要注册的数据库上下文类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="optionsAction">配置数据库提供程序及其他上下文选项的委托。</param>
    /// <returns>同一个服务集合，便于链式注册。</returns>
    /// <remarks>审计拦截器从当前上下文作用域解析，不会在注册阶段创建临时服务提供程序。</remarks>
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

    /// <summary>扫描数据库上下文所在程序集并注册默认查询仓储与聚合仓储。</summary>
    /// <typeparam name="TDbContext">仓储使用的数据库上下文类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <returns>同一个服务集合。</returns>
    /// <remarks>仅导出的、非抽象且实现 <see cref="IEntity"/> 的实体会被扫描；只有聚合根会注册写仓储。</remarks>
    public static IServiceCollection WithRepository<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        return services.TryAddRepository<TDbContext>([typeof(TDbContext).Assembly]);
    }

    /// <summary>从指定程序集扫描实体，并以不覆盖已有自定义注册的方式添加默认仓储。</summary>
    /// <typeparam name="TDbContext">仓储使用的数据库上下文类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="assemblies">要扫描的程序集序列；重复程序集会被去重。</param>
    /// <returns>同一个服务集合。</returns>
    /// <remarks>调用方可在本方法之前注册具体仓储，使 <c>TryAdd</c> 保留该自定义实现。</remarks>
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
