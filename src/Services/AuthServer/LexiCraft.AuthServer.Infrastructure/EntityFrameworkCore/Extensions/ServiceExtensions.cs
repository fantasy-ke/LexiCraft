using System.Reflection;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.Shared;
using LexiCraft.AuthServer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Extensions;

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
        services.WithDbAccess<LexiCraftDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.AddInterceptors(new AuditableEntityInterceptor(services.BuildServiceProvider()));
#endif
        });
        services.Configure<ContextOption>(configuration.GetSection("DbContextOptions"));
        services.WithRepository<LexiCraftDbContext>();
        return services;
    }

}