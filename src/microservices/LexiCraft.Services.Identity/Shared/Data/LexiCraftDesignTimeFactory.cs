using BuildingBlocks.EntityFrameworkCore.Postgres;

namespace LexiCraft.Services.Identity.Shared.Data;

public class LexiCraftDesignTimeFactory(): DbContextDesignFactoryBase<LexiCraftDbContext>("PostgresOptions:ConnectionString");