using BuildingBlocks.Core.Extensions;
using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Interceptors;
using BuildingBlocks.EntityFrameworkCore.Postgres;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddIdentityStorage(this IHostApplicationBuilder builder)
    {
        var option = builder.Configuration.BindOptions<PostgresOptions>();

        builder.Services.WithDbAccess<LexiCraftDbContext>(options =>
        {
            options.UseNpgsql(option.ConnectionString);
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.AddInterceptors(new AuditableEntityInterceptor(builder.Services.BuildServiceProvider()));
#endif
        });
        builder.Services.AddConfigurationOptions<ContextOption>("DbContextOptions");
        builder.Services.WithRepository<LexiCraftDbContext>();
        

        return builder;
    }
}