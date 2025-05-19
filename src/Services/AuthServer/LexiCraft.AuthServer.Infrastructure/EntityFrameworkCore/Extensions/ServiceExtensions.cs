using System.Reflection;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Shared;
using LexiCraft.AuthServer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// 添加数据库访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection WithDbAccess<TDbContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TDbContext : DbContext
    {
        // Npgsql 6.0.0 之后的版本需要设置以下两个开关，否则会导致时间戳字段的值不正确
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

        services.AddDbContext<TDbContext>(optionsAction);
        // services.AddDbContextPool<TDbContext>(optionsAction);
        services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();
        // services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
    
    public static IServiceCollection WithRepository<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        // 获取当前类所在的程序集
        var currentAssembly = Assembly.GetExecutingAssembly();
        // 获取当前程序集引用的所有程序集名称
        var referencedAssemblyNames = currentAssembly.GetReferencedAssemblies().ToList();
    
        // 存储当前程序集和引用的程序集
        var assemblies = new List<Assembly> { currentAssembly };
    
        referencedAssemblyNames.ForEach(assemblyName =>
        {
            // 加载引用的程序集
            var referencedAssembly = Assembly.Load(assemblyName);
            assemblies.Add(referencedAssembly);
        });
        services.TryAddRepository<TDbContext>(assemblies.Distinct());
        return services;
    }
    
    /// <summary>
    /// 添加数据库访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection WithLexiCraftDbAccess(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.WithDbAccess<LexiCraftDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });
        services.Configure<ContextOption>(configuration.GetSection("DbContextOptions"));
        services.WithRepository<LexiCraftDbContext>();
        return services;
    }
    
    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
        where TDbContext : DbContext
    {

        var allTypes = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
        var entityTypes = allTypes.Where(type => type.IsEntity());
        foreach (var entityType in entityTypes)
        {
            var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(entityType);
            services.TryAddAddDefaultRepository(repositoryInterfaceType, GetRepositoryImplementationType(typeof(TDbContext), entityType));
        }

        return services;
    }

    private static bool IsEntity(this Type type)
        => type.IsClass && !type.IsGenericType && !type.IsAbstract && typeof(IEntity).IsAssignableFrom(type);

    private static void TryAddAddDefaultRepository(this IServiceCollection services, Type repositoryInterfaceType,
        Type repositoryImplementationType)
    {
        if (repositoryInterfaceType.IsAssignableFrom(repositoryImplementationType))
        {
            services.TryAddScoped(repositoryInterfaceType, repositoryImplementationType);
        }
    }

    private static Type GetRepositoryImplementationType(Type dbContextType, Type entityType)
        => typeof(Repository<,>).MakeGenericType(dbContextType, entityType);

}