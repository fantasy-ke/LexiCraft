using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Abstractions;

public interface IDataSeeder<in TContext>
    where TContext : DbContext
{
    Task SeedAsync(TContext context, CancellationToken cancellationToken = default);
}
