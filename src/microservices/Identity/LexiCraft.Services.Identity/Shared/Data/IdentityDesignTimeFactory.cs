using BuildingBlocks.EntityFrameworkCore.Postgres.DesignTime;

namespace LexiCraft.Services.Identity.Shared.Data;

public class IdentityDesignTimeFactory()
    : DbContextDesignFactoryBase<IdentityDbContext>("PostgresOptions:ConnectionString", 2);