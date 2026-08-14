using System.Reflection;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.EntityFrameworkCore.Postgres.Configuration;
using BuildingBlocks.EntityFrameworkCore.Transactions;
using BuildingBlocks.Extensions;
using BuildingBlocks.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddPostgresDbContext<TDbContext>(
        this IHostApplicationBuilder builder,
        string? connectionStringName,
        Assembly? migrationAssembly = null,
        Action<IHostApplicationBuilder>? action = null,
        Action<DbContextOptionsBuilder>? dbContextBuilder = null,
        Action<PostgresOptions>? configurator = null)
        where TDbContext : DbContext
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

        builder.Services.AddValidationOptions(configurator: configurator);
        var postgresOptions = builder.Configuration.BindOptions(configurator);

        var configuredConnectionString = string.IsNullOrWhiteSpace(connectionStringName)
            ? null
            : builder.Configuration.GetConnectionString(connectionStringName);
        var connectionString = !string.IsNullOrWhiteSpace(configuredConnectionString)
            ? configuredConnectionString
            : postgresOptions.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Postgres connection string '{connectionStringName}' or 'PostgresOptions.ConnectionString' was not configured.");

        builder.Services.TryAddScoped<AuditableEntityInterceptor>();
        builder.Services.AddDbContext<TDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(
                    connectionString,
                    sqlOptions =>
                    {
                        var name = migrationAssembly?.GetName().Name
                                   ?? postgresOptions.MigrationAssembly
                                   ?? typeof(TDbContext).Assembly.GetName().Name;

                        sqlOptions.MigrationsAssembly(name);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    })
                .UseSnakeCaseNamingConvention();

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
            dbContextBuilder?.Invoke(options);
        });

        action?.Invoke(builder);
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork<TDbContext>>();
        builder.Services.WithRepository<TDbContext>();

        return builder;
    }
}
