using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Data.Repositories;

public class WordRepository(VocabularyDbContext context) 
    : Repository<VocabularyDbContext, Word>(context), IWordRepository
{
    public async Task<Word?> GetByIdAsync(long id)
    {
        return await FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Word>> GetByListIdAsync(long wordListId)
    {
        return await QueryNoTracking<WordListItem>()
            .Where(x => x.WordListId == wordListId)
            .Join(QueryNoTracking<Word>(),
                li => li.WordId,
                w => w.Id,
                (li, w) => w)
            .ToListAsync();
    }
}
