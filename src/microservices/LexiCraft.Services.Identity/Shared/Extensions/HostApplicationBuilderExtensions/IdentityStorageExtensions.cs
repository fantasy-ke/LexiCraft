using BuildingBlocks.Core.Extensions;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.EntityFrameworkCore.Postgres;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddIdentityStorage(this IHostApplicationBuilder builder)
    {
        builder.AddPostgresDbContext<IdentityDbContext>(
            connectionStringName: nameof(PostgresOptions),
            action: app =>
            {
                if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Aspire"))
                {
                    app.AddMigration<IdentityDbContext, IdentityDbDataSeeder>();
                }
                else
                {
                    app.AddMigration<IdentityDbContext>();
                }
            }
        );
        
        builder.Services.AddConfigurationOptions<ContextOption>();

        return builder;
    }
}