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
    private static readonly Lazy<bool> MongoMappingsConfigured =
        new(ConfigureMongoMappings, LazyThreadSafetyMode.ExecutionAndPublication);

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
        builder.Services.AddScoped<IResilienceService, MongoResilienceService>();
        builder.Services.AddScoped<TContext>();
        builder.Services.AddScoped<IMongoDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IProblemCodeMapper, MongoDbProblemCodeMapper>());

        return builder;
    }

    public static IHostApplicationBuilder AddMongoRepository<TDbContext>(this IHostApplicationBuilder builder)
    {
        builder.Services.TryAddRepository<TDbContext>([typeof(TDbContext).Assembly]);
        return builder;
    }

    public static IServiceCollection TryAddRepository<TDbContext>(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
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
        BsonSerializer.RegisterSerializationProvider(new DateTimeSerializationProvider());
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.CSharpLegacy));
        ConventionRegistry.Register(
            "LexiCraft.MongoDB",
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