using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Words.Data.Repositories;

public class WordListRepository(VocabularyDbContext context)
    : Repository<VocabularyDbContext, WordList>(context), IWordListRepository
{
    public async Task<WordList?> GetByIdAsync(long id)
    {
        return await FirstOrDefaultAsync(x => x.Id == id);
    }
}