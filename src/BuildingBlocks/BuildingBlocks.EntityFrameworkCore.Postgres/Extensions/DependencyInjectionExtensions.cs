using System.Reflection;
using BuildingBlocks.Core.Extensions;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddPostgresDbContext<TDbContext>(
        this IHostApplicationBuilder builder,
        string? connectionStringName,
        Assembly? migrationAssembly = null,
        Action<IHostApplicationBuilder>? action = null,
        Action<DbContextOptionsBuilder>? dbContextBuilder = null,
        Action<PostgresOptions>? configurator = null
    )
        where TDbContext : DbContext
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Services.AddValidationOptions(configurator: configurator);

        var postgresOptions = builder.Configuration.BindOptions<PostgresOptions>();

        var connectionString =
            !string.IsNullOrWhiteSpace(connectionStringName)
            && !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(connectionStringName))
                ? builder.Configuration.GetConnectionString(connectionStringName)
                : postgresOptions.ConnectionString
                    ?? throw new InvalidOperationException(
                        $"Postgres connection string '{connectionStringName}' or `postgresOptions.ConnectionString` not found."
                    );

        builder.Services.AddDbContext<TDbContext>(
            (sp, options) =>
            {
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

                options
                    .UseNpgsql(
                        connectionString,
                        sqlOptions =>
                        {
                            var name =
                                migrationAssembly?.GetName().Name
                                ?? postgresOptions.MigrationAssembly
                                ?? typeof(TDbContext).Assembly.GetName().Name;

                            sqlOptions.MigrationsAssembly(name);
                            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        }
                    )
                    .UseSnakeCaseNamingConvention();

                options.AddInterceptors(
                    new AuditableEntityInterceptor(
                        builder.Services.BuildServiceProvider())
                );

                dbContextBuilder?.Invoke(options);
            }
        );

        action?.Invoke(builder);
        builder.Services.WithRepository<TDbContext>();

        return builder;
    }
}
