using BuildingBlocks.EntityFrameworkCore.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;

/// <summary>
///     把注册时提供的委托适配为作用域 <see cref="IDataSeeder{TContext}"/>；委托与上下文共享迁移工作器创建的作用域。
/// </summary>
internal sealed class DefaultDataSeeder<TContext>(
    IServiceProvider serviceProvider,
    Func<TContext, IServiceProvider, CancellationToken, Task> seeder) : IDataSeeder<TContext>
    where TContext : DbContext
{
    public Task SeedAsync(TContext context, CancellationToken cancellationToken = default)
    {
        return seeder(context, serviceProvider, cancellationToken);
    }
}