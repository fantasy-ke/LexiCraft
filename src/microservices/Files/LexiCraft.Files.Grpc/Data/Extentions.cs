using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Files.Grpc.Data;

/// <summary>
///     扩展方法
/// </summary>
public static class Extentions
{
    /// <summary>
    ///     添加数据库访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection WithLexiCraftDbAccess(this IServiceCollection services,
        IConfiguration configuration)
    {
        // services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.WithDbAccess<FilesDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("Database"));
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.AddInterceptors(new AuditableEntityInterceptor(services.BuildServiceProvider()));
#endif
        });
        // services.Configure<ContextOption>(configuration.GetSection("DbContextOptions"));
        services.WithRepository<FilesDbContext>();
        return services;
    }

    /// <summary>
    ///     应用数据库迁移
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseMigration(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        dbContext.Database.MigrateAsync();

        return app;
    }
}