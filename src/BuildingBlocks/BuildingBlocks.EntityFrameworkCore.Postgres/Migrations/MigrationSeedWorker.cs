using System.Diagnostics;
using BuildingBlocks.EntityFrameworkCore.Abstractions;
using BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;

/// <summary>在应用启动期间执行 EF Core 迁移与种子数据初始化。</summary>
/// <typeparam name="TContext">要迁移和初始化的数据库上下文类型。</typeparam>
/// <param name="serviceProvider">用于创建独立异步作用域的根服务提供程序。</param>
/// <remarks>
///     迁移和 seed 作为同一次提供程序执行策略委托执行，但该工作器不会额外创建数据库事务。
///     任一阶段失败都会记录错误、标记诊断活动并重新抛出，从而阻止应用在未知数据库状态下继续启动。
/// </remarks>
public class MigrationSeedWorker<TContext>(IServiceProvider serviceProvider) : IHostedService
    where TContext : DbContext
{
    private static readonly ActivitySource ActivitySource = new(MigrationExtensions.ActivitySourceName);

    /// <summary>创建异步作用域，并在执行策略中运行迁移和 seed。</summary>
    /// <param name="cancellationToken">宿主启动取消令牌，会传播到迁移和 seed。</param>
    /// <returns>迁移与 seed 全部完成时结束的任务。</returns>
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

    /// <summary>停止阶段无需执行附加操作。</summary>
    /// <param name="cancellationToken">宿主停止取消令牌。</param>
    /// <returns>已完成任务。</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // 必须先完成结构迁移再执行 seed；二者共享同一作用域和取消令牌，失败不能被吞掉。
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
