using BuildingBlocks.Persistence.Abstractions.Repositories;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IWordListRepository : IRepository<WordList>
{
    Task<WordList?> GetByIdAsync(long id);
}