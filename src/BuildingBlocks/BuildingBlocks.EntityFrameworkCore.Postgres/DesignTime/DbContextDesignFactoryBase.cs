using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.DesignTime;

/// <summary>为 EF Core 命令行工具提供 PostgreSQL 数据库上下文的设计时创建基类。</summary>
/// <typeparam name="TDbContext">要创建的数据库上下文类型。</typeparam>
/// <param name="connectionStringSection">配置中连接字符串值的完整键，例如 <c>ConnectionStrings:Fantasy</c>。</param>
/// <param name="paramCount">上下文构造函数参数总数；除第一个选项参数外，其余参数以 <see langword="null"/> 传入。</param>
/// <param name="env">可选环境名；为空时读取 <c>ASPNETCORE_ENVIRONMENT</c>，仍为空则使用 Development。</param>
/// <remarks>
///     工厂从工具进程的 <see cref="AppContext.BaseDirectory"/> 读取配置，并启用与运行时注册一致的旧时间戳兼容开关、
///     Npgsql 重试和 snake_case 命名。目标上下文必须具有与 <paramref name="paramCount"/> 匹配的可调用构造函数。
/// </remarks>
public abstract class DbContextDesignFactoryBase<TDbContext>(
    string connectionStringSection,
    int paramCount = 1,
    string? env = null) : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>根据设计时配置创建数据库上下文。</summary>
    /// <param name="args">EF Core 工具传入的参数；当前实现不解析该数组。</param>
    /// <returns>配置为 Npgsql 和 snake_case 命名的数据库上下文。</returns>
    /// <exception cref="InvalidOperationException">连接配置缺失或无法实例化上下文时抛出。</exception>
    public TDbContext CreateDbContext(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

        var environmentName =
            env ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetValue<string>(connectionStringSection);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Could not find a value for {connectionStringSection} section.");

        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(GetType().Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                })
            .UseSnakeCaseNamingConvention();

        var parameters = paramCount <= 1
            ? [optionsBuilder.Options]
            : new[] { optionsBuilder.Options }.Concat(Enumerable.Repeat<object?>(null, paramCount - 1)).ToArray();

        return (TDbContext)(Activator.CreateInstance(typeof(TDbContext), parameters) ??
                            throw new InvalidOperationException("Could not create instance of DbContext."));
    }
}
