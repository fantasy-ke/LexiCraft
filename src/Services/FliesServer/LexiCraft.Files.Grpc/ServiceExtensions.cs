using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using LexiCraft.Files.Grpc.Data;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Files.Grpc;

public static class ServiceExtensions
{
    /// <summary>
    /// 添加数据库访问
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

}