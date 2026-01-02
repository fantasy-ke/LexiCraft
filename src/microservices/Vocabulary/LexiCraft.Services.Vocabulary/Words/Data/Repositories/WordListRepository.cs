using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.Words.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.Words.Data.Repositories;

public class WordListRepository(VocabularyDbContext context) : IWordListRepository
{
    public async Task<WordList?> GetByIdAsync(long id)
    {
        return await context.WordLists.FirstOrDefaultAsync(x => x.Id == id);
    }

    public IQueryable<WordList> Query()
    {
        return context.WordLists.AsQueryable();
    }
}
