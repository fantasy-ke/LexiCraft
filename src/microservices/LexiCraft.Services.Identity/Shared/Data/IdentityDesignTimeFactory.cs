using BuildingBlocks.EntityFrameworkCore.Postgres;

namespace LexiCraft.Services.Identity.Shared.Data;

public class IdentityDesignTimeFactory(): DbContextDesignFactoryBase<IdentityDbContext>("PostgresOptions:ConnectionString", 2);