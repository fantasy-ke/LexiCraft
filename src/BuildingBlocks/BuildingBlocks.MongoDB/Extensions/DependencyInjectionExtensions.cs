using System.Reflection;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using BuildingBlocks.Extensions;
using BuildingBlocks.MongoDB.Configuration;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.MongoDB.Resilience;
using BuildingBlocks.MongoDB.Serialization;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace BuildingBlocks.MongoDB.Extensions;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddMongoDbContext<TContext>(this IHostApplicationBuilder builder,
        string? sectionName = null)
        where TContext : class, IMongoDbContext
    {
        // 1. Configure Options
        builder.Services.AddConfigurationOptions<MongoOptions>(sectionName ?? nameof(MongoOptions));
        
        var mongoOptions = builder.Configuration.BindOptions<MongoOptions>();

        var connectionString =
            mongoOptions.ConnectionString
            ?? throw new InvalidOperationException("`MongoOptions.ConnectionString` can't be null.");

        // 2. Register IMongoClient
        builder.Services.AddSingleton<IMongoClient>(sp =>
            CreateMongoDbClient(connectionString, mongoOptions, sp)
        );

        // 3. Register IMongoDatabase
        builder.AddMongoDatabase(connectionString);

        // 4. Register MongoDB-specific services
        builder.Services.AddSingleton<IMongoPerformanceMonitor, MongoPerformanceMonitor>();
        builder.Services.AddScoped<IResilienceService, MongoResilienceService>();

        // 5. Register DbContext
        builder.Services.AddScoped<TContext>();
        builder.Services.AddScoped<IMongoDbContext>(sp => sp.GetRequiredService<TContext>());

        // 6. Serialization
        BsonSerializer.RegisterSerializationProvider(new DateTimeSerializationProvider());
        
        return builder;
    }

    /// <summary>
    ///  创建MongoDbClient
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="mongoOptions"></param>
    /// <param name="sp"></param>
    /// <returns></returns>
    private static IMongoClient CreateMongoDbClient(
        string connectionString,
        MongoOptions mongoOptions,
        IServiceProvider sp
    )
    {
        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);

        // Configure connection pooling for performance
        clientSettings.MaxConnectionPoolSize = mongoOptions.MaxConnectionPoolSize;
        clientSettings.MinConnectionPoolSize = mongoOptions.MinConnectionPoolSize;
        clientSettings.MaxConnectionIdleTime = mongoOptions.MaxConnectionIdleTime;
        clientSettings.MaxConnectionLifeTime = mongoOptions.MaxConnectionLifeTime;
        clientSettings.ConnectTimeout = mongoOptions.ConnectTimeout;
        clientSettings.SocketTimeout = mongoOptions.SocketTimeout;
        clientSettings.ServerSelectionTimeout = mongoOptions.ServerSelectionTimeout;

        // Configure for concurrent operations
        clientSettings.ReadConcern = ReadConcern.Local;
        clientSettings.WriteConcern = WriteConcern.WMajority;

        if (!mongoOptions.DisableTracing)
        {
            clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        }

        clientSettings.LoggingSettings ??= new LoggingSettings(sp.GetService<ILoggerFactory>());

        return new MongoClient(clientSettings);
    }

    private static void AddMongoDatabase(this IHostApplicationBuilder builder, string? connectionString)
    {
        var mongoUrl = new MongoUrl(connectionString);
        builder.Services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(mongoUrl.DatabaseName));
    }


    /// <summary>
    ///  添加仓储
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IHostApplicationBuilder AddMongoRepository<TDbContext>(
        this IHostApplicationBuilder builder)
    {
        // 获取当前类所在的程序集
        // var currentAssembly = Assembly.GetExecutingAssembly();
        var currentAssembly = typeof(TDbContext).Assembly;
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
        builder.Services.TryAddRepository<TDbContext>(assemblies.Distinct());
        return builder;
    }

    /// <summary>
    ///  添加仓储
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        var allTypes = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
        var entityTypes = allTypes.Where(type => type.IsMongoEntity());
        foreach (var entityType in entityTypes)
        {
            var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(entityType);
            services.TryAddAddDefaultRepository(repositoryInterfaceType, GetRepositoryImplementationType(entityType));
        }

        return services;
    }

    private static bool IsMongoEntity(this Type type)
        => type is { IsClass: true, IsGenericType: false, IsAbstract: true } &&
           typeof(MongoEntity).IsAssignableFrom(type);

    private static void TryAddAddDefaultRepository(this IServiceCollection services, Type repositoryInterfaceType,
        Type repositoryImplementationType)
    {
        if (repositoryInterfaceType.IsAssignableFrom(repositoryImplementationType))
        {
            services.TryAddScoped(repositoryInterfaceType, repositoryImplementationType);
        }
    }

    private static Type GetRepositoryImplementationType(Type entityType)
        => typeof(MongoRepository<>).MakeGenericType(entityType);
}