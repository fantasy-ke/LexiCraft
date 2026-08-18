using System.Reflection;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Exceptions.Problem;
using BuildingBlocks.Extensions;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Configuration;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Errors;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.MongoDB.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using BuildingBlocks.MongoDB.Serialization;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace BuildingBlocks.MongoDB.Extensions;

/// <summary>提供 MongoDB 客户端、上下文、仓储、序列化约定和诊断服务的注册扩展。</summary>
public static class DependencyInjectionExtensions
{
    private static readonly Lazy<bool> MongoMappingsConfigured =
        new(ConfigureMongoMappings, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>从指定配置节注册 MongoDB 客户端、数据库、上下文、弹性服务和性能监控。</summary>
    /// <typeparam name="TContext">实现 <see cref="IMongoDbContext"/> 的作用域上下文类型。</typeparam>
    /// <param name="builder">应用宿主构建器。</param>
    /// <param name="sectionName">配置节名称；为空时使用 <see cref="MongoOptions"/> 类型名。</param>
    /// <returns>同一个宿主构建器。</returns>
    /// <exception cref="InvalidOperationException">连接字符串缺失、格式无效、没有数据库名或连接池边界无效时抛出。</exception>
    /// <remarks>
    ///     BSON serializer 与 convention 是进程级全局状态，只会线程安全地初始化一次。客户端和数据库为单例，
    ///     上下文、session 与仓储为作用域服务。该方法会用 MongoDB 映射器替换默认问题码映射器。
    /// </remarks>
    public static IHostApplicationBuilder AddMongoDbContext<TContext>(
        this IHostApplicationBuilder builder,
        string? sectionName = null)
        where TContext : class, IMongoDbContext
    {
        var configurationSection = sectionName ?? nameof(MongoOptions);
        builder.Services.AddConfigurationOptions<MongoOptions>(configurationSection);

        var mongoOptions = builder.Configuration.BindOptions<MongoOptions>(configurationSection);
        var mongoUrl = ValidateOptions(mongoOptions, configurationSection);

        _ = MongoMappingsConfigured.Value;

        builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
            CreateMongoDbClient(mongoUrl, mongoOptions, serviceProvider));
        builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
            serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));

        builder.Services.AddSingleton<IMongoPerformanceMonitor, MongoPerformanceMonitor>();
        builder.Services.AddScoped<IMongoResilienceService, MongoResilienceService>();
        builder.Services.AddScoped<TContext>();
        builder.Services.AddScoped<IMongoDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IProblemCodeMapper, MongoDbProblemCodeMapper>());

        return builder;
    }

    /// <summary>扫描上下文所在程序集并注册默认 MongoDB 查询仓储和聚合写仓储。</summary>
    /// <typeparam name="TDbContext">仓储使用的 MongoDB 上下文类型。</typeparam>
    /// <param name="builder">应用宿主构建器。</param>
    /// <returns>同一个宿主构建器。</returns>
    public static IHostApplicationBuilder AddMongoRepository<TDbContext>(this IHostApplicationBuilder builder)
        where TDbContext : class, IMongoDbContext
    {
        builder.Services.TryAddRepository<TDbContext>([typeof(TDbContext).Assembly]);
        return builder;
    }

    /// <summary>从指定程序集扫描 Mongo 实体，并在没有自定义注册时添加默认仓储。</summary>
    /// <typeparam name="TDbContext">仓储使用的 MongoDB 上下文类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="assemblies">要扫描的程序集序列；重复程序集会被去重。</param>
    /// <returns>同一个服务集合。</returns>
    /// <remarks>
    ///     所有公开、非抽象 <see cref="MongoEntity"/> 注册查询仓储；只有聚合根注册写仓储。
    ///     默认集合名为实体 CLR 类型名，不会自动复数化或转换大小写。
    /// </remarks>
    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
        where TDbContext : class, IMongoDbContext
    {
        var entityTypes = assemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsMongoEntity());

        foreach (var entityType in entityTypes)
        {
            var queryRepositoryInterfaceType = typeof(IQueryRepository<>).MakeGenericType(entityType);
            var queryRepositoryImplementationType = typeof(MongoQueryRepository<>).MakeGenericType(entityType);
            services.TryAddScoped(queryRepositoryInterfaceType, queryRepositoryImplementationType);

            if (!typeof(IAggregateRoot).IsAssignableFrom(entityType)) continue;

            var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(entityType);
            services.TryAddDefaultRepository(
                repositoryInterfaceType,
                typeof(MongoRepository<>).MakeGenericType(entityType));
        }

        return services;
    }

    private static MongoUrl ValidateOptions(MongoOptions options, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException(
                $"MongoDB connection string is missing in configuration section '{sectionName}'.");

        MongoUrl mongoUrl;
        try
        {
            mongoUrl = new MongoUrl(options.ConnectionString);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"MongoDB connection string in configuration section '{sectionName}' is invalid.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            throw new InvalidOperationException(
                $"MongoDB connection string in configuration section '{sectionName}' must include a database name.");

        if (options.MaxConnectionPoolSize <= 0)
            throw new InvalidOperationException("MongoDB MaxConnectionPoolSize must be greater than zero.");

        if (options.MinConnectionPoolSize < 0 ||
            options.MinConnectionPoolSize > options.MaxConnectionPoolSize)
            throw new InvalidOperationException(
                "MongoDB MinConnectionPoolSize must be between zero and MaxConnectionPoolSize.");

        return mongoUrl;
    }

    private static IMongoClient CreateMongoDbClient(
        MongoUrl mongoUrl,
        MongoOptions mongoOptions,
        IServiceProvider serviceProvider)
    {
        var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
        clientSettings.MaxConnectionPoolSize = mongoOptions.MaxConnectionPoolSize;
        clientSettings.MinConnectionPoolSize = mongoOptions.MinConnectionPoolSize;
        clientSettings.MaxConnectionIdleTime = mongoOptions.MaxConnectionIdleTime;
        clientSettings.MaxConnectionLifeTime = mongoOptions.MaxConnectionLifeTime;
        clientSettings.ConnectTimeout = mongoOptions.ConnectTimeout;
        clientSettings.SocketTimeout = mongoOptions.SocketTimeout;
        clientSettings.ServerSelectionTimeout = mongoOptions.ServerSelectionTimeout;
        clientSettings.ReadConcern = ReadConcern.Local;
        clientSettings.WriteConcern = WriteConcern.WMajority;

        if (!mongoOptions.DisableTracing)
            clientSettings.ClusterConfigurator = clusterBuilder =>
                clusterBuilder.Subscribe(new DiagnosticsActivityEventSubscriber());

        clientSettings.LoggingSettings ??= new LoggingSettings(serviceProvider.GetService<ILoggerFactory>());
        return new MongoClient(clientSettings);
    }

    private static bool ConfigureMongoMappings()
    {
        // BSON 注册是进程级且不可撤销的全局状态；外层 Lazy 保证多 Host/TestServer 并发时只执行一次。
        BsonSerializer.RegisterSerializationProvider(new DateTimeSerializationProvider());
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.CSharpLegacy));
        ConventionRegistry.Register(
            "BuildingBlocks.MongoDB",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreIfDefaultConvention(false)
            },
            _ => true);
        return true;
    }

    private static bool IsMongoEntity(this Type type)
    {
        return type is { IsClass: true, IsGenericType: false, IsAbstract: false } &&
               typeof(MongoEntity).IsAssignableFrom(type);
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
