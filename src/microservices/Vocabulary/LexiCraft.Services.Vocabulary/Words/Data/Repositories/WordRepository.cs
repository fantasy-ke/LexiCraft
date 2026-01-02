using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Data.Repositories;

public class WordRepository(VocabularyDbContext context) : IWordRepository
{
    public async Task<Word?> GetByIdAsync(long id)
    {
        return await context.Words.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Word>> GetByListIdAsync(long wordListId)
    {
        return await context.WordListItems
            .Where(x => x.WordListId == wordListId)
            .Join(context.Words,
                li => li.WordId,
                w => w.Id,
                (li, w) => w)
            .ToListAsync();
    }

    public IQueryable<Word> Query()
    {
        return context.Words.AsQueryable();
    }
}
