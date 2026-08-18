using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Abstractions;

/// <summary>定义指定 EF Core 上下文的种子数据写入器。</summary>
/// <typeparam name="TContext">要初始化数据的 <see cref="DbContext"/> 类型。</typeparam>
public interface IDataSeeder<in TContext>
    where TContext : DbContext
{
    /// <summary>异步写入种子数据。</summary>
    /// <param name="context">已解析且可用于迁移后写入的数据库上下文。</param>
    /// <param name="cancellationToken">应用停止或调用方取消时使用的令牌。</param>
    /// <returns>表示种子操作的任务。</returns>
    /// <remarks>实现应自行保证重复执行安全，并把取消令牌传给所有数据库 I/O。</remarks>
    Task SeedAsync(TContext context, CancellationToken cancellationToken = default);
}
