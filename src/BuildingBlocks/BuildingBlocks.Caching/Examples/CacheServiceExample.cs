using BuildingBlocks.Caching.Abstractions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Examples;

/// <summary>
///     缓存服务使用示例
/// </summary>
public class CacheServiceExample
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheServiceExample> _logger;

    public CacheServiceExample(ICacheService cacheService, ILogger<CacheServiceExample> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     基础缓存操作示例
    /// </summary>
    public async Task BasicCacheOperationsExample()
    {
        _logger.LogInformation("开始基础缓存操作示例");

        // 设置缓存
        await _cacheService.SetAsync("user:123", new { Id = 123, Name = "张三", Email = "zhangsan@example.com" });
        _logger.LogInformation("设置用户缓存: user:123");

        // 获取缓存
        var user = await _cacheService.GetAsync<object>("user:123");
        _logger.LogInformation("获取用户缓存: {User}", user);

        // 检查缓存是否存在
        var exists = await _cacheService.ExistsAsync("user:123");
        _logger.LogInformation("用户缓存是否存在: {Exists}", exists);

        // 删除缓存
        var removed = await _cacheService.RemoveAsync("user:123");
        _logger.LogInformation("删除用户缓存结果: {Removed}", removed);
    }

    /// <summary>
    ///     GetOrSet 操作示例
    /// </summary>
    public async Task GetOrSetExample()
    {
        _logger.LogInformation("开始 GetOrSet 操作示例");

        var userId = 456;
        var user = await _cacheService.GetOrSetAsync(
            $"user:{userId}",
            async () =>
            {
                _logger.LogInformation("缓存未命中，从数据库获取用户: {UserId}", userId);
                // 模拟从数据库获取用户
                await Task.Delay(100);
                return new { Id = userId, Name = "李四", Email = "lisi@example.com" };
            },
            options =>
            {
                options.Expiry = TimeSpan.FromMinutes(30);
                options.EnableLock = true;
            });

        _logger.LogInformation("获取或设置用户: {User}", user);

        // 第二次调用应该从缓存获取
        var cachedUser = await _cacheService.GetOrSetAsync(
            $"user:{userId}",
            async () =>
            {
                _logger.LogInformation("这不应该被调用，因为缓存应该命中");
                await Task.Delay(100);
                return new { Id = userId, Name = "李四", Email = "lisi@example.com" };
            });

        _logger.LogInformation("第二次获取用户（应该来自缓存）: {User}", cachedUser);
    }

    /// <summary>
    ///     Hash 缓存操作示例
    /// </summary>
    public async Task HashCacheExample()
    {
        _logger.LogInformation("开始 Hash 缓存操作示例");

        var profileKey = "user_profile:789";
        var queryFields = new[] { "user_id", "username", "email", "last_login" };

        var profile = await _cacheService.GetOrSetHashAsync<UserProfile>(
            profileKey,
            queryFields,
            hashData =>
            {
                // 从 Hash 数据解析用户档案
                if (hashData.TryGetValue("user_id", out var userIdStr) &&
                    int.TryParse(userIdStr, out var userId))
                    return new UserProfile
                    {
                        UserId = userId,
                        Username = hashData.GetValueOrDefault("username", ""),
                        Email = hashData.GetValueOrDefault("email", ""),
                        LastLogin = hashData.TryGetValue("last_login", out var lastLoginStr) &&
                                    DateTime.TryParse(lastLoginStr, out var lastLogin)
                            ? lastLogin
                            : null
                    };
                return null;
            },
            async () =>
            {
                _logger.LogInformation("Hash 缓存未命中，构建完整用户档案");
                // 模拟从数据库构建完整用户档案
                await Task.Delay(200);
                return new Dictionary<string, string>
                {
                    ["user_id"] = "789",
                    ["username"] = "wangwu",
                    ["email"] = "wangwu@example.com",
                    ["last_login"] = DateTime.UtcNow.AddHours(-2).ToString("O"),
                    ["created_at"] = DateTime.UtcNow.AddDays(-30).ToString("O"),
                    ["status"] = "active"
                };
            },
            options =>
            {
                options.Expiry = TimeSpan.FromHours(2);
                options.EnableLock = true;
                // 动态调整 Hash 缓存 TTL
                options.AdjustExpiryForHash = (originalExpiry, hashData) =>
                {
                    // 如果用户最近登录，延长缓存时间
                    if (hashData.TryGetValue("last_login", out var lastLoginStr) &&
                        DateTime.TryParse(lastLoginStr, out var lastLogin))
                    {
                        var timeSinceLogin = DateTime.UtcNow - lastLogin;
                        if (timeSinceLogin < TimeSpan.FromHours(1)) return TimeSpan.FromHours(4); // 延长到 4 小时
                    }

                    return originalExpiry;
                };
            });

        _logger.LogInformation("获取用户档案: {Profile}", profile);
    }

    /// <summary>
    ///     混合缓存模式示例
    /// </summary>
    public async Task HybridCacheExample()
    {
        _logger.LogInformation("开始混合缓存模式示例");

        var productId = 999;
        var product = await _cacheService.GetOrSetAsync(
            $"product:{productId}",
            async () =>
            {
                _logger.LogInformation("从数据库获取产品: {ProductId}", productId);
                await Task.Delay(150);
                return new { Id = productId, Name = "示例产品", Price = 99.99m };
            },
            options =>
            {
                options.UseDistributed = true;
                options.UseLocal = true;
                options.Expiry = TimeSpan.FromHours(1); // 分布式缓存 1 小时
                options.LocalExpiry = TimeSpan.FromMinutes(10); // 本地缓存 10 分钟
                options.EnableLock = true;
            });

        _logger.LogInformation("获取产品（混合缓存）: {Product}", product);

        // 多次调用验证本地缓存优先级
        for (var i = 0; i < 3; i++)
        {
            var cachedProduct = await _cacheService.GetAsync<object>($"product:{productId}");
            _logger.LogInformation("第 {Index} 次获取产品（应该来自本地缓存）: {Product}", i + 1, cachedProduct);
        }
    }

    /// <summary>
    ///     异常处理和降级策略示例
    /// </summary>
    public async Task ErrorHandlingExample()
    {
        _logger.LogInformation("开始异常处理和降级策略示例");

        var result = await _cacheService.GetOrSetAsync(
            "error_test_key",
            async () =>
            {
                _logger.LogInformation("模拟工厂方法执行");
                await Task.Delay(50);
                return "工厂方法结果";
            },
            options =>
            {
                options.HideErrors = true;
                options.FallbackToFactory = true;
                options.FallbackToDefault = true;
                options.DefaultValue = "默认降级值";
                options.OnError = ex =>
                {
                    _logger.LogWarning("缓存操作异常回调: {Message}", ex.Message);
                    return "异常回调结果";
                };
                // 使用不存在的 Redis 实例来触发异常
                options.RedisInstanceName = "nonexistent_instance";
            });

        _logger.LogInformation("异常处理结果: {Result}", result);
    }

    /// <summary>
    ///     详细的降级策略示例
    /// </summary>
    public async Task FallbackStrategiesExample()
    {
        _logger.LogInformation("开始详细的降级策略示例");

        // 示例 1: 工厂方法降级策略
        _logger.LogInformation("=== 工厂方法降级策略示例 ===");
        var factoryResult = await _cacheService.GetOrSetAsync(
            "factory_fallback_test",
            async () =>
            {
                _logger.LogInformation("执行工厂方法作为降级策略");
                await Task.Delay(100);
                return new { Message = "来自工厂方法的数据", Timestamp = DateTime.UtcNow };
            },
            options =>
            {
                options.HideErrors = true;
                options.FallbackToFactory = true; // 启用工厂方法降级
                options.EnableLock = true;
                options.LockAcquireTimeout = TimeSpan.FromMilliseconds(1); // 极短超时，模拟锁获取失败
                options.RedisInstanceName = "test_instance"; // 可能不存在的实例
            });
        _logger.LogInformation("工厂方法降级结果: {Result}", factoryResult);

        // 示例 2: 默认值降级策略
        _logger.LogInformation("=== 默认值降级策略示例 ===");
        var defaultResult = await _cacheService.GetAsync<string>(
            "nonexistent_key",
            options =>
            {
                options.HideErrors = true;
                options.FallbackToDefault = true; // 启用默认值降级
                options.DefaultValue = "这是默认降级值";
                options.RedisInstanceName = "nonexistent_instance"; // 触发异常
            });
        _logger.LogInformation("默认值降级结果: {Result}", defaultResult);

        // 示例 3: 自定义函数降级策略
        _logger.LogInformation("=== 自定义函数降级策略示例 ===");
        var customResult = await _cacheService.GetAsync<object>(
            "custom_fallback_test",
            options =>
            {
                options.HideErrors = true;
                options.FallbackFunction = (key, operation) =>
                {
                    _logger.LogInformation("执行自定义降级函数: Key={Key}, Operation={Operation}", key, operation);
                    return new
                    {
                        Message = "来自自定义降级函数",
                        Key = key,
                        Operation = operation,
                        Timestamp = DateTime.UtcNow
                    };
                };
                options.RedisInstanceName = "nonexistent_instance"; // 触发异常
            });
        _logger.LogInformation("自定义函数降级结果: {Result}", customResult);

        // 示例 4: 组合降级策略（优先级测试）
        _logger.LogInformation("=== 组合降级策略优先级示例 ===");
        var combinedResult = await _cacheService.GetAsync<string>(
            "combined_fallback_test",
            options =>
            {
                options.HideErrors = true;
                // 同时启用多种降级策略，测试优先级
                options.FallbackToDefault = true;
                options.DefaultValue = "默认值降级";
                options.FallbackFunction = (key, operation) =>
                {
                    _logger.LogInformation("自定义函数降级被调用");
                    return "自定义函数降级";
                };
                options.OnError = ex =>
                {
                    _logger.LogInformation("异常回调被调用: {Message}", ex.Message);
                    return "异常回调降级";
                };
                options.RedisInstanceName = "nonexistent_instance"; // 触发异常
            });
        _logger.LogInformation("组合降级策略结果: {Result}", combinedResult);

        // 示例 5: 透明异常模式（不隐藏异常）
        _logger.LogInformation("=== 透明异常模式示例 ===");
        try
        {
            var transparentResult = await _cacheService.GetAsync<string>(
                "transparent_error_test",
                options =>
                {
                    options.HideErrors = false; // 不隐藏异常
                    options.RedisInstanceName = "nonexistent_instance"; // 触发异常
                });
            _logger.LogInformation("透明异常模式结果: {Result}", transparentResult);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("捕获到预期的异常: {Message}", ex.Message);
        }
    }

    /// <summary>
    ///     Hash 缓存降级策略示例
    /// </summary>
    public async Task HashFallbackExample()
    {
        _logger.LogInformation("开始 Hash 缓存降级策略示例");

        var hashResult = await _cacheService.GetOrSetHashAsync<UserProfile>(
            "hash_fallback_test",
            new[] { "user_id", "username", "email" },
            hashData =>
            {
                _logger.LogInformation("解析 Hash 数据: {HashData}", hashData);
                return new UserProfile
                {
                    UserId = int.TryParse(hashData.GetValueOrDefault("user_id", "0"), out var id) ? id : 0,
                    Username = hashData.GetValueOrDefault("username", "unknown"),
                    Email = hashData.GetValueOrDefault("email", "unknown@example.com")
                };
            },
            async () =>
            {
                _logger.LogInformation("Hash 工厂方法被调用作为降级策略");
                await Task.Delay(100);
                return new Dictionary<string, string>
                {
                    ["user_id"] = "999",
                    ["username"] = "fallback_user",
                    ["email"] = "fallback@example.com",
                    ["created_at"] = DateTime.UtcNow.ToString("O")
                };
            },
            options =>
            {
                options.HideErrors = true;
                options.FallbackToFactory = true;
                options.EnableLock = true;
                options.LockAcquireTimeout = TimeSpan.FromMilliseconds(1); // 模拟锁获取失败
                options.FallbackFunction = (key, operation) =>
                {
                    _logger.LogInformation("Hash 自定义降级函数被调用");
                    return new UserProfile
                    {
                        UserId = 888,
                        Username = "custom_fallback_user",
                        Email = "custom@example.com"
                    };
                };
            });

        _logger.LogInformation("Hash 降级策略结果: {Result}", hashResult);
    }

    /// <summary>
    ///     用户档案数据模型
    /// </summary>
    public class UserProfile
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
    }
}