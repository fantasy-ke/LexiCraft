namespace BuildingBlocks.EntityFrameworkCore.Postgres;

public class PostgresOptions
{
    public string? ConnectionString { get; set; }
    public string? MigrationAssembly { get; set; }
}
