using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IWordListRepository
{
    Task<WordList?> GetByIdAsync(long id);
    IQueryable<WordList> Query();
}
