using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Redis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

/// <summary>
///     缓存服务实现，提供高级缓存操作
/// </summary>
internal sealed partial class CacheService : ICacheService
{
    private readonly IRedisCacheStore _distributedCacheService;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<CacheService> _logger;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    ///     初始化缓存服务
    /// </summary>
    /// <param name="distributedCacheService">分布式缓存服务</param>
    /// <param name="memoryCache">内存缓存</param>
    /// <param name="lockProvider">分布式锁提供者</param>
    /// <param name="logger">日志记录器</param>
    public CacheService(
        IRedisCacheStore distributedCacheService,
        IMemoryCache memoryCache,
        IDistributedLockProvider lockProvider,
        ILogger<CacheService> logger)
    {
        _distributedCacheService =
            distributedCacheService ?? throw new ArgumentNullException(nameof(distributedCacheService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
