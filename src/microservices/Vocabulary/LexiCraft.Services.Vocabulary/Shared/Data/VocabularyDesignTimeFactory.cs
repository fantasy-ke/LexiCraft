using BuildingBlocks.EntityFrameworkCore.Postgres.DesignTime;

namespace LexiCraft.Services.Vocabulary.Shared.Data;

public class VocabularyDesignTimeFactory()
    : DbContextDesignFactoryBase<VocabularyDbContext>("PostgresOptions:ConnectionString", 2);