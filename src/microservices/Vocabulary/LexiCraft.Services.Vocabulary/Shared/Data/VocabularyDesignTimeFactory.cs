using BuildingBlocks.EntityFrameworkCore.Postgres;

namespace LexiCraft.Services.Vocabulary.Shared.Data;

public class VocabularyDesignTimeFactory()
    : DbContextDesignFactoryBase<VocabularyDbContext>("PostgresOptions:ConnectionString", 2);