using BuildingBlocks.EntityFrameworkCore.Abstractions;
using BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Extensions;

/// <summary>提供 PostgreSQL 数据库启动迁移与种子数据注册扩展。</summary>
public static class MigrationExtensions
{
    /// <summary>迁移活动使用的诊断源名称。</summary>
    public const string ActivitySourceName = "DbMigrations";

    extension(IHostApplicationBuilder builder)
    {
        /// <summary>注册只执行 EF Core 迁移、不写入额外种子数据的启动工作器。</summary>
        /// <typeparam name="TContext">要迁移的数据库上下文类型。</typeparam>
        /// <returns>同一个宿主构建器。</returns>
        public IHostApplicationBuilder AddMigration<TContext>()
            where TContext : DbContext
        {
            return builder.AddMigration<TContext>((_, _, _) => Task.CompletedTask);
        }

        /// <summary>注册启动迁移工作器和不接收取消令牌的种子委托。</summary>
        /// <typeparam name="TContext">要迁移和初始化的数据库上下文类型。</typeparam>
        /// <param name="seeder">迁移成功后执行的种子数据委托。</param>
        /// <returns>同一个宿主构建器。</returns>
        /// <remarks>为完整传播应用停止信号，新增代码应优先使用接收 <see cref="CancellationToken"/> 的重载。</remarks>
        public IHostApplicationBuilder AddMigration<TContext>(
            Func<TContext, IServiceProvider, Task> seeder)
            where TContext : DbContext
        {
            return builder.AddMigration<TContext>((context, serviceProvider, _) => seeder(context, serviceProvider));
        }

        /// <summary>注册启动迁移工作器和支持取消的种子委托。</summary>
        /// <typeparam name="TContext">要迁移和初始化的数据库上下文类型。</typeparam>
        /// <param name="seeder">迁移成功后执行的种子数据委托。</param>
        /// <returns>同一个宿主构建器。</returns>
        /// <remarks>迁移或种子委托失败会从 Hosted Service 向上抛出并终止应用启动。</remarks>
        public IHostApplicationBuilder AddMigration<TContext>(
            Func<TContext, IServiceProvider, CancellationToken, Task> seeder)
            where TContext : DbContext
        {
            builder.Services.AddScoped<IDataSeeder<TContext>>(serviceProvider =>
                new DefaultDataSeeder<TContext>(serviceProvider, seeder));
            builder.Services.AddHostedService<MigrationSeedWorker<TContext>>();
            return builder;
        }

        /// <summary>注册启动迁移工作器和指定的种子数据实现。</summary>
        /// <typeparam name="TContext">要迁移和初始化的数据库上下文类型。</typeparam>
        /// <typeparam name="TDbSeeder">作用域种子数据服务类型。</typeparam>
        /// <returns>同一个宿主构建器。</returns>
        /// <remarks>种子实现应可重复执行，并将收到的取消令牌传播到底层数据库调用。</remarks>
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
