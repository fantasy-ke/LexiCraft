using BuildingBlocks.EntityFrameworkCore.Extensions;
using BuildingBlocks.EntityFrameworkCore.Postgres;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Data.Repositories;
using LexiCraft.Services.Vocabulary.UserStates.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Vocabulary.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddVocabularyStorage(this IHostApplicationBuilder builder)
    {
        builder.AddPostgresDbContext<VocabularyDbContext>(
            connectionStringName: nameof(PostgresOptions),
            action: app =>
            {
                if (app.Environment.IsDevelopment() || app.Environment.IsAspireRun())
                {
                    app.AddMigration<VocabularyDbContext, VocabularyDbDataSeeder>();
                }
                else
                {
                    app.AddMigration<VocabularyDbContext>();
                }
            }
        );
        
        builder.Services.AddConfigurationOptions<ContextOption>();
        
        AddRepositoryStorage(builder);

        return builder;
    }

    private static void AddRepositoryStorage(IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IWordRepository, WordRepository>();
        builder.Services.AddTransient<IWordListRepository, WordListRepository>();
        builder.Services.AddTransient<IWordListItemRepository, WordListItemRepository>();
        builder.Services.AddTransient<IUserWordStateRepository, UserWordStateRepository>();
    }
}
