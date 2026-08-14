using BuildingBlocks.EntityFrameworkCore.Abstractions;
using BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;

public static class MigrationExtensions
{
    public const string ActivitySourceName = "DbMigrations";

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddMigration<TContext>()
            where TContext : DbContext
        {
            return builder.AddMigration<TContext>((_, _, _) => Task.CompletedTask);
        }

        public IHostApplicationBuilder AddMigration<TContext>(
            Func<TContext, IServiceProvider, Task> seeder)
            where TContext : DbContext
        {
            return builder.AddMigration<TContext>((context, serviceProvider, _) => seeder(context, serviceProvider));
        }

        public IHostApplicationBuilder AddMigration<TContext>(
            Func<TContext, IServiceProvider, CancellationToken, Task> seeder)
            where TContext : DbContext
        {
            builder.Services.AddScoped<IDataSeeder<TContext>>(serviceProvider =>
                new DefaultDataSeeder<TContext>(serviceProvider, seeder));
            builder.Services.AddHostedService<MigrationSeedWorker<TContext>>();
            return builder;
        }

        public IHostApplicationBuilder AddMigration<TContext, TDbSeeder>()
            where TContext : DbContext
            where TDbSeeder : class, IDataSeeder<TContext>
        {
            builder.Services.AddScoped<IDataSeeder<TContext>, TDbSeeder>();
            builder.Services.AddHostedService<MigrationSeedWorker<TContext>>();
            return builder;
        }
    }
}
