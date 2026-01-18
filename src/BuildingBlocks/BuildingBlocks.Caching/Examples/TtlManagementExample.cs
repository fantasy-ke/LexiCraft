using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Examples;

/// <summary>
/// TTL 管理功能示例
/// 演示需求 2.1-2.5 的 TTL 继承和覆盖逻辑
/// </summary>
public class TtlManagementExample
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<TtlManagementExample> _logger;

    public TtlManagementExample(ICacheService cacheService, ILogger<TtlManagementExample> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// 演示需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// </summary>
    public async Task DemonstrateUnifiedExpiryAsync()
    {
        _logger.LogInformation("=== 演示统一过期时间 (需求 2.1) ===");

        // 使用默认的全局过期时间
        await _cacheService.SetAsync("user:123", new { Name = "张三", Age = 25 });
        
        // 自定义全局过期时间
        await _cacheService.SetAsync("user:456", new { Name = "李四", Age = 30 }, options =>
        {
            options.Expiry = TimeSpan.FromMinutes(60); // 自定义 60 分钟过期
        });

        _logger.LogInformation("设置了两个缓存项，使用不同的全局过期时间");
    }

    /// <summary>
    /// 演示需求 2.2 & 2.3: 本地缓存独立过期时间和 TTL 继承
    /// </summary>
    public async Task DemonstrateLocalExpiryInheritanceAsync()
    {
        _logger.LogInformation("=== 演示本地缓存 TTL 继承 (需求 2.2 & 2.3) ===");

        // 需求 2.3: 本地缓存继承全局过期时间
        await _cacheService.SetAsync("product:123", new { Name = "商品A", Price = 99.99m }, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.Expiry = TimeSpan.FromHours(2); // 全局 2 小时
            // LocalExpiry 未设置，本地缓存将继承全局过期时间
        });

        // 需求 2.2: 本地缓存使用独立的 TTL
        await _cacheService.SetAsync("product:456", new { Name = "商品B", Price = 199.99m }, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.Expiry = TimeSpan.FromHours(2); // 全局 2 小时
            options.LocalExpiry = TimeSpan.FromMinutes(15); // 本地缓存 15 分钟
        });

        _logger.LogInformation("设置了混合缓存，演示了 TTL 继承和独立设置");
    }

    /// <summary>
    /// 演示需求 2.4: 动态调整普通缓存 TTL
    /// </summary>
    public async Task DemonstrateDynamicTtlAdjustmentAsync()
    {
        _logger.LogInformation("=== 演示动态 TTL 调整 (需求 2.4) ===");

        // 根据数据内容动态调整 TTL
        await _cacheService.SetAsync("dynamic:user:789", new { Name = "王五", VipLevel = "Gold" }, options =>
        {
            options.Expiry = TimeSpan.FromMinutes(30); // 基础 30 分钟
            
            // 需求 2.4: 动态调整过期时间委托
            options.AdjustExpiryForValue = (originalExpiry, value) =>
            {
                // 根据用户 VIP 等级调整缓存时间
                if (value is { } obj && obj.ToString()!.Contains("Gold"))
                {
                    return originalExpiry.Add(TimeSpan.FromMinutes(30)); // VIP 用户延长 30 分钟
                }
                return originalExpiry;
            };
        });

        // 根据数据大小动态调整 TTL
        var largeData = new string('A', 10000); // 大数据
        await _cacheService.SetAsync("dynamic:large:data", largeData, options =>
        {
            options.Expiry = TimeSpan.FromMinutes(60);
            
            options.AdjustExpiryForValue = (originalExpiry, value) =>
            {
                // 大数据缩短缓存时间以节省内存
                if (value is string str && str.Length > 5000)
                {
                    return TimeSpan.FromMinutes(15); // 大数据只缓存 15 分钟
                }
                return originalExpiry;
            };
        });

        _logger.LogInformation("设置了动态 TTL 调整的缓存项");
    }

    /// <summary>
    /// 演示需求 2.5: Hash 缓存 TTL 调整
    /// </summary>
    public async Task DemonstrateHashTtlAdjustmentAsync()
    {
        _logger.LogInformation("=== 演示 Hash 缓存 TTL 调整 (需求 2.5) ===");

        // Hash 缓存的动态 TTL 调整
        await _cacheService.GetOrSetHashAsync<Dictionary<string, string>>(
            "user_profile:999",
            new[] { "user_id", "username", "email" },
            hashData => hashData,
            async () =>
            {
                // 模拟从数据库获取用户资料
                await Task.Delay(100);
                return new Dictionary<string, string>
                {
                    ["user_id"] = "999",
                    ["username"] = "赵六",
                    ["email"] = "zhaoliu@example.com",
                    ["last_login"] = DateTime.UtcNow.ToString("O"),
                    ["profile_completeness"] = "80" // 资料完整度
                };
            },
            options =>
            {
                options.Expiry = TimeSpan.FromHours(1); // 基础 1 小时
                
                // 需求 2.5: 基于 Hash 内容调整过期时间
                options.AdjustExpiryForHash = (originalExpiry, hashData) =>
                {
                    // 根据用户资料完整度调整缓存时间
                    if (hashData.TryGetValue("profile_completeness", out var completeness) &&
                        int.TryParse(completeness, out var percentage))
                    {
                        if (percentage >= 90)
                        {
                            return originalExpiry.Add(TimeSpan.FromHours(2)); // 完整资料延长缓存
                        }
                        else if (percentage < 50)
                        {
                            return TimeSpan.FromMinutes(15); // 不完整资料缩短缓存
                        }
                    }
                    return originalExpiry;
                };
            });

        _logger.LogInformation("设置了 Hash 缓存的动态 TTL 调整");
    }

    /// <summary>
    /// 演示 TTL 验证和错误处理
    /// </summary>
    public async Task DemonstrateTtlValidationAsync()
    {
        _logger.LogInformation("=== 演示 TTL 验证和错误处理 ===");

        // 测试无效的 TTL 值
        await _cacheService.SetAsync("test:invalid:ttl", "测试数据", options =>
        {
            options.Expiry = TimeSpan.Zero; // 无效的过期时间
            options.LocalExpiry = TimeSpan.FromSeconds(-10); // 无效的本地过期时间
            options.UseLocal = true;
        });

        // 测试动态 TTL 调整异常处理
        await _cacheService.SetAsync("test:exception:ttl", "测试数据", options =>
        {
            options.Expiry = TimeSpan.FromMinutes(30);
            
            options.AdjustExpiryForValue = (originalExpiry, value) =>
            {
                // 故意抛出异常来测试异常处理
                throw new InvalidOperationException("TTL 调整失败");
            };
        });

        _logger.LogInformation("测试了 TTL 验证和异常处理");
    }

    /// <summary>
    /// 运行所有 TTL 管理示例
    /// </summary>
    public async Task RunAllExamplesAsync()
    {
        _logger.LogInformation("开始运行 TTL 管理功能示例...");

        await DemonstrateUnifiedExpiryAsync();
        await DemonstrateLocalExpiryInheritanceAsync();
        await DemonstrateDynamicTtlAdjustmentAsync();
        await DemonstrateHashTtlAdjustmentAsync();
        await DemonstrateTtlValidationAsync();

        _logger.LogInformation("TTL 管理功能示例运行完成！");
    }
}