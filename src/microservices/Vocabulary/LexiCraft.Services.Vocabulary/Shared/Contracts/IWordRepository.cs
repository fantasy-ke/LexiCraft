using BuildingBlocks.Domain;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IWordRepository : IQueryRepository<Word>
{
    Task<Word?> GetByIdAsync(long id);
    Task<List<Word>> GetByListIdAsync(long wordListId);
}