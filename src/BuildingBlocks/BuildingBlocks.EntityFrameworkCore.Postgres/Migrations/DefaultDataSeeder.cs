using BuildingBlocks.EntityFrameworkCore.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Postgres.Migrations;

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