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

/// <summary>提供 PostgreSQL EF Core 上下文的依赖注入注册扩展。</summary>
public static class DependencyInjectionExtensions
{
    /// <summary>注册 Npgsql 数据库上下文、审计拦截器、工作单元和默认仓储。</summary>
    /// <typeparam name="TDbContext">要注册的数据库上下文类型。</typeparam>
    /// <param name="builder">应用宿主构建器。</param>
    /// <param name="connectionStringName"><c>ConnectionStrings</c> 下的连接名称；为空时直接使用选项回退值。</param>
    /// <param name="migrationAssembly">可选迁移程序集；优先级高于配置中的迁移程序集名称。</param>
    /// <param name="action">完成数据库服务注册后调用的附加宿主配置委托。</param>
    /// <param name="dbContextBuilder">在 Npgsql、命名约定和审计拦截器配置后调用的上下文选项委托。</param>
    /// <param name="configurator">用于补充或覆盖绑定后 <see cref="PostgresOptions"/> 的委托。</param>
    /// <returns>同一个宿主构建器。</returns>
    /// <exception cref="InvalidOperationException">命名连接字符串和 <see cref="PostgresOptions.ConnectionString"/> 均为空时抛出。</exception>
    /// <remarks>
    ///     连接字符串优先读取 <c>ConnectionStrings:{connectionStringName}</c>，再回退到
    ///     <see cref="PostgresOptions.ConnectionString"/>。Npgsql 执行策略最多重试 5 次、单次最长延迟 10 秒；
    ///     自动重试不能替代业务幂等或正确的事务执行策略用法。
    /// </remarks>
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
