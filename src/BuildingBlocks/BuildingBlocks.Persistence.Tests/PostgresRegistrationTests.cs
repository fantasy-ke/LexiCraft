using BuildingBlocks.EntityFrameworkCore;
using BuildingBlocks.EntityFrameworkCore.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Persistence.Tests;

public class PostgresRegistrationTests
{
    [Fact]
    public void Registration_uses_PostgresOptions_connection_string_fallback()
    {
        const string connectionString = "Host=localhost;Database=persistence_tests;Username=test";
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PostgresOptions:ConnectionString"] = connectionString
        });

        builder.AddPostgresDbContext<TestDbContext>(null);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(connectionString, context.Database.GetConnectionString());
    }

    [Fact]
    public void Registration_fails_fast_when_connection_string_is_missing()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.AddPostgresDbContext<TestDbContext>("Missing"));

        Assert.Contains("was not configured", exception.Message);
    }

    [Fact]
    public async Task Migration_worker_propagates_seeding_failure()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"lexicraft-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite($"Data Source={databasePath};Pooling=False"));
        services.AddScoped<IDataSeeder<TestDbContext>, ThrowingSeeder>();

        try
        {
            await using (var provider = services.BuildServiceProvider())
            {
                var worker = new MigrationSeedWorker<TestDbContext>(provider);
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    worker.StartAsync(CancellationToken.None));
                Assert.Equal("seed failed", exception.Message);
            }
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    private sealed class ThrowingSeeder : IDataSeeder<TestDbContext>
    {
        public Task SeedAsync(TestDbContext context, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("seed failed");
        }
    }
}
