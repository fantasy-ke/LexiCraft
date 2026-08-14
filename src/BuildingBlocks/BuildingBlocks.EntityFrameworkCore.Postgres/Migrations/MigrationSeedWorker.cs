using System.Diagnostics;
using BuildingBlocks.EntityFrameworkCore.Abstractions;
using BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;

public class MigrationSeedWorker<TContext>(IServiceProvider serviceProvider) : IHostedService
    where TContext : DbContext
{
    private static readonly ActivitySource ActivitySource = new(MigrationExtensions.ActivitySourceName);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var scopeServiceProvider = scope.ServiceProvider;
        var seeder = scopeServiceProvider.GetRequiredService<IDataSeeder<TContext>>();
        var logger = scopeServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scopeServiceProvider.GetRequiredService<TContext>();

        using var activity = ActivitySource.StartActivity($"Migration operation {typeof(TContext).Name}");

        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(
                token => ExecuteAsync(seeder, context, token),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while migrating the database used on context {DbContextName}",
                typeof(TContext).Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task ExecuteAsync(
        IDataSeeder<TContext> seeder,
        TContext context,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"Migrating {typeof(TContext).Name}");

        await context.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(context, cancellationToken);
    }
}
