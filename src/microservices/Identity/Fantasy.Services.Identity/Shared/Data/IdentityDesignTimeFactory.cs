using BuildingBlocks.EntityFrameworkCore.Postgres.DesignTime;

namespace Fantasy.Services.Identity.Shared.Data;

public class IdentityDesignTimeFactory()
    : DbContextDesignFactoryBase<IdentityDbContext>("PostgresOptions:ConnectionString", 2);