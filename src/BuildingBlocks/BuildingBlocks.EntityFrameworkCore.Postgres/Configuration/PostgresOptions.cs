namespace BuildingBlocks.EntityFrameworkCore.Postgres.Configuration;

public class PostgresOptions
{
    public string? ConnectionString { get; set; }
    public string? MigrationAssembly { get; set; }
}
