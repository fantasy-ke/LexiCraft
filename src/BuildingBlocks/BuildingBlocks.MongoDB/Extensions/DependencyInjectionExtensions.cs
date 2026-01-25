using System.Reflection;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Exceptions.Problem;
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
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
        // 1. 配置选项
        builder.Services.AddConfigurationOptions<MongoOptions>(sectionName ?? nameof(MongoOptions));

        var mongoOptions = builder.Configuration.BindOptions<MongoOptions>();

        var connectionString =
            mongoOptions.ConnectionString
            ?? throw new InvalidOperationException("`MongoOptions.ConnectionString` can't be null.");

        // 2. 注册IMongoClient
        builder.Services.AddSingleton<IMongoClient>(sp =>
            CreateMongoDbClient(connectionString, mongoOptions, sp)
        );

        // 3. 注册IMongoDatabase
        builder.AddMongoDatabase(connectionString);

        // 4. 注册MongoDB特定服务
        builder.Services.AddSingleton<IMongoPerformanceMonitor, MongoPerformanceMonitor>();
        builder.Services.AddScoped<IResilienceService, MongoResilienceService>();

        BsonSerializer.RegisterSerializationProvider(new DateTimeSerializationProvider());
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.CSharpLegacy));


        RegisterConventions();
        // 5. 注册数据库上下文
        builder.Services.AddScoped<TContext>();
        builder.Services.AddScoped<IMongoDbContext>(sp => sp.GetRequiredService<TContext>());
        builder.Services.AddSingleton<IProblemCodeMapper, MongoDbProblemCodeMapper>();

        return builder;
    }

    /// <summary>
    ///     创建MongoDbClient
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="mongoOptions">MongoDB选项配置</param>
    /// <param name="sp">服务提供程序</param>
    /// <returns>MongoClient实例</returns>
    private static IMongoClient CreateMongoDbClient(
        string connectionString,
        MongoOptions mongoOptions,
        IServiceProvider sp
    )
    {
        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);

        // 配置连接池以提高性能
        clientSettings.MaxConnectionPoolSize = mongoOptions.MaxConnectionPoolSize;
        clientSettings.MinConnectionPoolSize = mongoOptions.MinConnectionPoolSize;
        clientSettings.MaxConnectionIdleTime = mongoOptions.MaxConnectionIdleTime;
        clientSettings.MaxConnectionLifeTime = mongoOptions.MaxConnectionLifeTime;
        clientSettings.ConnectTimeout = mongoOptions.ConnectTimeout;
        clientSettings.SocketTimeout = mongoOptions.SocketTimeout;
        clientSettings.ServerSelectionTimeout = mongoOptions.ServerSelectionTimeout;

        // 配置并发操作
        clientSettings.ReadConcern = ReadConcern.Local;
        clientSettings.WriteConcern = WriteConcern.WMajority;

        if (!mongoOptions.DisableTracing)
            clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());

        clientSettings.LoggingSettings ??= new LoggingSettings(sp.GetService<ILoggerFactory>());

        return new MongoClient(clientSettings);
    }

    private static void AddMongoDatabase(this IHostApplicationBuilder builder, string? connectionString)
    {
        var mongoUrl = new MongoUrl(connectionString);
        builder.Services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(mongoUrl.DatabaseName));
    }

    private static void RegisterConventions()
    {
        ConventionRegistry.Register(
            "conventions",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreIfDefaultConvention(false)
            },
            _ => true
        );
    }


    /// <summary>
    ///     添加仓储
    /// </summary>
    /// <param name="builder">主机应用程序构建器</param>
    /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
    /// <returns>更新后的主机应用程序构建器</returns>
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
    ///     添加仓储
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">程序集集合</param>
    /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
    /// <returns>更新后的服务集合</returns>
    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        var allTypes = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
        var entityTypes = allTypes.Where(type => type.IsMongoEntity());
        foreach (var entityType in entityTypes)
        {
            // 注册只读仓储 (适用于所有实体)
            var queryRepositoryInterfaceType = typeof(IQueryRepository<>).MakeGenericType(entityType);
            var queryRepositoryImplementationType = typeof(MongoQueryRepository<>).MakeGenericType(entityType);
            services.TryAddScoped(queryRepositoryInterfaceType, queryRepositoryImplementationType);

            // 如果有 Resilient 版本，可以考虑替换或额外注册，这里保持默认实现

            // 注册聚合根仓储 (仅适用于聚合根)
            if (typeof(IAggregateRoot).IsAssignableFrom(entityType))
            {
                var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(entityType);
                // 默认使用非弹性版本，或者根据需求选择
                services.TryAddAddDefaultRepository(repositoryInterfaceType,
                    GetRepositoryImplementationType(entityType));
                // 如果需要弹性版本，通常会覆盖注入
                // services.TryAddAddDefaultRepository(repositoryInterfaceType, GetResilientRepositoryImplementationType(entityType));
            }
        }

        return services;
    }

    private static bool IsMongoEntity(this Type type)
    {
        return type is { IsClass: true, IsGenericType: false, IsAbstract: false } &&
               typeof(MongoEntity).IsAssignableFrom(type);
    }

    private static void TryAddAddDefaultRepository(this IServiceCollection services, Type repositoryInterfaceType,
        Type repositoryImplementationType)
    {
        if (repositoryInterfaceType.IsAssignableFrom(repositoryImplementationType))
            services.TryAddScoped(repositoryInterfaceType, repositoryImplementationType);
    }

    private static Type GetRepositoryImplementationType(Type entityType)
    {
        return typeof(MongoRepository<>).MakeGenericType(entityType);
    }

    private static Type GetResilientRepositoryImplementationType(Type entityType)
    {
        return typeof(ResilientMongoRepository<>).MakeGenericType(entityType);
    }
}