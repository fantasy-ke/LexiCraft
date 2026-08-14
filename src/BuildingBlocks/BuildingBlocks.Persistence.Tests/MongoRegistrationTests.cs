using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Exceptions.Problem;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Context;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Errors;
using BuildingBlocks.MongoDB.Repositories;
using BuildingBlocks.MongoDB.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using BuildingBlocks.Persistence.Abstractions.Repositories;

namespace BuildingBlocks.Persistence.Tests;

public class MongoRegistrationTests
{
    [Fact]
    public void Registration_binds_the_requested_configuration_section()
    {
        var builder = CreateBuilder("CustomMongo", "mongodb://localhost/custom_database");

        builder.AddMongoDbContext<TestMongoDbContext>("CustomMongo");

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var context = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();

        Assert.Equal("custom_database", database.DatabaseNamespace.DatabaseName);
        Assert.IsType<TestMongoDbContext>(context);
    }

    [Fact]
    public void Registration_can_be_called_for_multiple_hosts_without_duplicate_mapping_errors()
    {
        var firstBuilder = CreateBuilder("MongoOne", "mongodb://localhost/database_one");
        var secondBuilder = CreateBuilder("MongoTwo", "mongodb://localhost/database_two");

        firstBuilder.AddMongoDbContext<TestMongoDbContext>("MongoOne");
        secondBuilder.AddMongoDbContext<TestMongoDbContext>("MongoTwo");

        using var firstHost = firstBuilder.Build();
        using var secondHost = secondBuilder.Build();
        Assert.NotNull(firstHost.Services.GetRequiredService<IMongoClient>());
        Assert.NotNull(secondHost.Services.GetRequiredService<IMongoClient>());
    }

    [Fact]
    public void Registration_resolves_the_unified_default_repository()
    {
        var builder = CreateBuilder("MongoRepository", "mongodb://localhost/repository_database");
        builder.AddMongoDbContext<TestMongoDbContext>("MongoRepository");
        builder.Services.TryAddRepository<TestMongoDbContext>([typeof(MongoRegistrationAggregate).Assembly]);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<MongoRegistrationAggregate>>();

        Assert.IsType<MongoRepository<MongoRegistrationAggregate>>(repository);
    }

    [Fact]
    public void Registration_replaces_the_default_problem_mapper_without_duplicates()
    {
        var builder = CreateBuilder("MongoMapper", "mongodb://localhost/mapper_database");
        builder.Services.AddSingleton<IProblemCodeMapper, DefaultProblemCodeMapper>();

        builder.AddMongoDbContext<TestMongoDbContext>("MongoMapper");
        builder.AddMongoDbContext<TestMongoDbContext>("MongoMapper");

        using var host = builder.Build();
        var mapper = Assert.Single(host.Services.GetServices<IProblemCodeMapper>());

        Assert.IsType<MongoDbProblemCodeMapper>(mapper);
        Assert.Equal(StatusCodes.Status400BadRequest, mapper.GetMappedStatusCodes(new ArgumentException()));
    }
    [Fact]
    public void Registration_preserves_conventions_for_embedded_document_types()
    {
        var builder = CreateBuilder("MongoConventions", "mongodb://localhost/convention_database");

        builder.AddMongoDbContext<TestMongoDbContext>("MongoConventions");

        var document = new EmbeddedDocument
        {
            DisplayName = "example",
            State = EmbeddedState.Active
        }.ToBsonDocument();

        Assert.Equal("example", document["displayName"].AsString);
        Assert.Equal(nameof(EmbeddedState.Active), document["state"].AsString);
    }

    [Fact]
    public void Registration_requires_a_database_name()
    {
        var builder = CreateBuilder("MongoWithoutDatabase", "mongodb://localhost");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.AddMongoDbContext<TestMongoDbContext>("MongoWithoutDatabase"));

        Assert.Contains("must include a database name", exception.Message);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 6)]
    public void Registration_rejects_invalid_pool_limits(int maxPoolSize, int minPoolSize)
    {
        var builder = CreateBuilder("InvalidPool", "mongodb://localhost/pool_database");
        builder.Configuration["InvalidPool:MaxConnectionPoolSize"] = maxPoolSize.ToString();
        builder.Configuration["InvalidPool:MinConnectionPoolSize"] = minPoolSize.ToString();

        Assert.Throws<InvalidOperationException>(() =>
            builder.AddMongoDbContext<TestMongoDbContext>("InvalidPool"));
    }

    private static HostApplicationBuilder CreateBuilder(string sectionName, string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{sectionName}:ConnectionString"] = connectionString
        });
        return builder;
    }

    private sealed class EmbeddedDocument
    {
        public string DisplayName { get; init; } = string.Empty;

        public EmbeddedState State { get; init; }
    }

    private enum EmbeddedState
    {
        Active
    }

    private sealed class TestMongoDbContext(IMongoDatabase database, IMongoClient client)
        : MongoDbContext(database, client);
}
