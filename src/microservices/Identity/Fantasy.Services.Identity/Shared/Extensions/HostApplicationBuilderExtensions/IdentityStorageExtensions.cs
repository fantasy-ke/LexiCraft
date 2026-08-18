using BuildingBlocks.EntityFrameworkCore.Postgres.Configuration;
using BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using Fantasy.Services.Identity.Identity.Data.Repositories;
using Fantasy.Services.Identity.Shared.Contracts;
using Fantasy.Services.Identity.Shared.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fantasy.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddStorage(this IHostApplicationBuilder builder)
    {
        AddIdentityStorage(builder);
        AddRepositoryStorage(builder);

        return builder;
    }

    public static IHostApplicationBuilder AddIdentityStorage(IHostApplicationBuilder builder)
    {
        builder.AddPostgresDbContext<IdentityDbContext>(
            nameof(PostgresOptions),
            action: app =>
            {
                if (app.Environment.IsDevelopment() || app.Environment.IsAspireRun())
                    app.AddMigration<IdentityDbContext, IdentityDbDataSeeder>();
                else
                    app.AddMigration<IdentityDbContext>();
            }
        );

        builder.Services.AddConfigurationOptions<ContextOption>();

        return builder;
    }


    private static void AddRepositoryStorage(IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IUserRepository, UserRepository>();
        builder.Services.AddTransient<IUserPermissionRepository, UserPermissionRepository>();
    }
}