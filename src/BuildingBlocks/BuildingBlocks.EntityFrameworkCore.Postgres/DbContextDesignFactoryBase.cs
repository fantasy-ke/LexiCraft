using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace BuildingBlocks.EntityFrameworkCore.Postgres;

public abstract class DbContextDesignFactoryBase<TDbContext>(string connectionStringSection, 
    int paramCount = 1,
    string? env = null)
    : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext(string[] args)
    {
        Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");

        var environmentName = env ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory ?? "")
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", true) // it is optional
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var connectionStringSectionValue = configuration.GetValue<string>(connectionStringSection);

        if (string.IsNullOrWhiteSpace(connectionStringSectionValue))
        {
            throw new InvalidOperationException($"Could not find a value for {connectionStringSection} section.");
        }

        Console.WriteLine($"ConnectionString  section value is : {connectionStringSectionValue}");

        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(
                connectionStringSectionValue,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(GetType().Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                }
            )//命名
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        // 创建参数数组，第一个参数是options，其余参数为null
        var parameters = paramCount <= 1 
            ? [optionsBuilder.Options]
            : new[] { optionsBuilder.Options }.Concat(Enumerable.Repeat<object?>(null, paramCount - 1)).ToArray();

        return (TDbContext)(Activator.CreateInstance(typeof(TDbContext), parameters) ?? throw new InvalidOperationException("Could not create instance of DbContext."));
    }
}