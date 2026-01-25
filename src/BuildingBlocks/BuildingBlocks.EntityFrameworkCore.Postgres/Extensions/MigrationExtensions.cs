using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres;

public static class MigrationExtensions
{
    public static readonly string ActivitySourceName = "DbMigrations";

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddMigration<TContext>()
            where TContext : DbContext
        {
            return builder.AddMigration<TContext>((_, _) => Task.CompletedTask);
        }

        public IHostApplicationBuilder AddMigration<TContext>(Func<TContext, IServiceProvider, Task> seeder
        )
            where TContext : DbContext
        {
            builder.Services.AddScoped<IDataSeeder<TContext>>(sp => new DefaultDataSeeder<TContext>(sp, seeder));

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

internal class DefaultDataSeeder<TContext>(IServiceProvider sp, Func<TContext, IServiceProvider, Task> seeder)
    : IDataSeeder<TContext>
    where TContext : DbContext
{
    public async Task SeedAsync(TContext context)
    {
        await seeder(context, sp);
    }
}