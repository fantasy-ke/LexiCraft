using BuildingBlocks.EntityFrameworkCore.Abstractions;

namespace LexiCraft.Services.Vocabulary.Shared.Data;

public class VocabularyDbDataSeeder : IDataSeeder<VocabularyDbContext>
{
    public Task SeedAsync(VocabularyDbContext context, CancellationToken cancellationToken = default)
    {
        // 初始种子数据可以在此添加
        return Task.CompletedTask;
    }
}