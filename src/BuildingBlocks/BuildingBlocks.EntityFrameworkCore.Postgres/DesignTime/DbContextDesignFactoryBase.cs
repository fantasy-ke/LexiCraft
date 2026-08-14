using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.DesignTime;

public abstract class DbContextDesignFactoryBase<TDbContext>(
    string connectionStringSection,
    int paramCount = 1,
    string? env = null) : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

        var environmentName =
            env ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetValue<string>(connectionStringSection);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Could not find a value for {connectionStringSection} section.");

        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(GetType().Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                })
            .UseSnakeCaseNamingConvention();

        var parameters = paramCount <= 1
            ? [optionsBuilder.Options]
            : new[] { optionsBuilder.Options }.Concat(Enumerable.Repeat<object?>(null, paramCount - 1)).ToArray();

        return (TDbContext)(Activator.CreateInstance(typeof(TDbContext), parameters) ??
                            throw new InvalidOperationException("Could not create instance of DbContext."));
    }
}
