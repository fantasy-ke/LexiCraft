using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Caching.Extensions;

public static class CachingServiceExtensions
{
    public static IServiceCollection WithRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfigurationOptions<RedisCacheOptions>("RedisCache");
        var cacheOption = services.BuildServiceProvider().GetRequiredService<RedisCacheOptions>();

        if (!cacheOption.Enable) return services;

        services.AddZRedis(cacheOption, options => { options.Capacity = 6; });
        services.AddSingleton<ICacheManager, CacheManager>();

        return services;
    }
}
