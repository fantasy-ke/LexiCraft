using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.DistributedLock;
using BuildingBlocks.Caching.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Examples;

/// <summary>
/// 分布式锁使用示例
/// </summary>
public class DistributedLockExample
{
    /// <summary>
    /// 运行分布式锁示例
    /// </summary>
    public static async Task RunAsync()
    {
        // 创建日志工厂
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var providerLogger = loggerFactory.CreateLogger<RedisDistributedLockProvider>();
        var lockLogger = loggerFactory.CreateLogger<RedisDistributedLock>();
        var factoryLogger = loggerFactory.CreateLogger<RedisConnectionFactory>();

        // 配置 Redis 连接选项
        var redisOptions = new RedisConnectionOptions
        {
            DefaultConnectionString = "localhost:6379"
        };

        var options = Options.Create(redisOptions);

        try
        {
            // 创建 Redis 连接工厂
            using var connectionFactory = new RedisConnectionFactory(options, factoryLogger);

            // 创建分布式锁提供者
            var lockProvider = new RedisDistributedLockProvider(connectionFactory, providerLogger, lockLogger);

            Console.WriteLine("=== 分布式锁示例 ===");

            // 示例 1: 基本锁获取和释放
            await BasicLockExample(lockProvider);

            // 示例 2: 锁超时示例
            await LockTimeoutExample(lockProvider);

            // 示例 3: 并发锁竞争示例
            await ConcurrentLockExample(lockProvider);

            Console.WriteLine("所有示例执行完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例执行失败: {ex.Message}");
            Console.WriteLine("请确保 Redis 服务器正在运行并且可以连接到 localhost:6379");
        }
    }

    /// <summary>
    /// 基本锁获取和释放示例
    /// </summary>
    private static async Task BasicLockExample(IDistributedLockProvider lockProvider)
    {
        Console.WriteLine("\n--- 基本锁获取和释放示例 ---");

        const string lockKey = "example:basic";
        var lockTimeout = TimeSpan.FromSeconds(10);
        var acquireTimeout = TimeSpan.FromSeconds(5);

        // 获取锁
        var distributedLock = await lockProvider.TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout);

        if (distributedLock != null)
        {
            Console.WriteLine($"✓ 成功获取锁: {lockKey}");
            Console.WriteLine($"  锁值: {distributedLock.LockValue}");
            Console.WriteLine($"  过期时间: {distributedLock.ExpiresAt}");
            Console.WriteLine($"  锁是否有效: {distributedLock.IsValid}");

            // 模拟一些工作
            await Task.Delay(1000);

            // 释放锁
            var released = await distributedLock.ReleaseAsync();
            Console.WriteLine($"✓ 锁释放结果: {released}");

            await distributedLock.DisposeAsync();
        }
        else
        {
            Console.WriteLine($"✗ 获取锁失败: {lockKey}");
        }
    }

    /// <summary>
    /// 锁超时示例
    /// </summary>
    private static async Task LockTimeoutExample(IDistributedLockProvider lockProvider)
    {
        Console.WriteLine("\n--- 锁超时示例 ---");

        const string lockKey = "example:timeout";
        var lockTimeout = TimeSpan.FromSeconds(2); // 短超时时间
        var acquireTimeout = TimeSpan.FromSeconds(1);

        // 第一次获取锁
        var lock1 = await lockProvider.TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout);
        if (lock1 != null)
        {
            Console.WriteLine($"✓ 第一次获取锁成功: {lockKey}");

            // 立即尝试再次获取同一个锁（应该失败）
            var lock2 = await lockProvider.TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout);
            if (lock2 == null)
            {
                Console.WriteLine("✓ 第二次获取锁失败（预期行为）");
            }
            else
            {
                Console.WriteLine("✗ 第二次获取锁成功（意外行为）");
                await lock2.DisposeAsync();
            }

            // 等待锁过期
            Console.WriteLine("等待锁过期...");
            await Task.Delay(lockTimeout.Add(TimeSpan.FromMilliseconds(500)));

            // 锁过期后再次尝试获取
            var lock3 = await lockProvider.TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout);
            if (lock3 != null)
            {
                Console.WriteLine("✓ 锁过期后重新获取成功");
                await lock3.DisposeAsync();
            }
            else
            {
                Console.WriteLine("✗ 锁过期后重新获取失败");
            }

            await lock1.DisposeAsync();
        }
    }

    /// <summary>
    /// 并发锁竞争示例
    /// </summary>
    private static async Task ConcurrentLockExample(IDistributedLockProvider lockProvider)
    {
        Console.WriteLine("\n--- 并发锁竞争示例 ---");

        const string lockKey = "example:concurrent";
        var lockTimeout = TimeSpan.FromSeconds(5);
        var acquireTimeout = TimeSpan.FromSeconds(2);

        // 启动多个并发任务尝试获取同一个锁
        var tasks = new Task[3];
        for (int i = 0; i < tasks.Length; i++)
        {
            int taskId = i + 1;
            tasks[i] = Task.Run(async () =>
            {
                Console.WriteLine($"任务 {taskId}: 尝试获取锁...");
                var distributedLock = await lockProvider.TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout);

                if (distributedLock != null)
                {
                    Console.WriteLine($"✓ 任务 {taskId}: 成功获取锁");
                    
                    // 模拟工作
                    await Task.Delay(1000);
                    
                    await distributedLock.ReleaseAsync();
                    Console.WriteLine($"✓ 任务 {taskId}: 锁已释放");
                    await distributedLock.DisposeAsync();
                }
                else
                {
                    Console.WriteLine($"✗ 任务 {taskId}: 获取锁失败");
                }
            });
        }

        // 等待所有任务完成
        await Task.WhenAll(tasks);
        Console.WriteLine("✓ 所有并发任务完成");
    }
}