using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

public interface IDataSeeder<in TContext>
    where TContext : DbContext
{
    Task SeedAsync(TContext context);
}