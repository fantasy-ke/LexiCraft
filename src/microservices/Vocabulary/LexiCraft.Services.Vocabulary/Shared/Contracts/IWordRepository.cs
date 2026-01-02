using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IWordRepository
{
    Task<Word?> GetByIdAsync(long id);
    Task<List<Word>> GetByListIdAsync(long wordListId);
    IQueryable<Word> Query();
}
