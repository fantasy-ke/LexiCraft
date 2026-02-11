using BuildingBlocks.Domain;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using LexiCraft.Services.Vocabulary.Words.Models;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IUserWordStateRepository : IQueryRepository<UserWordState>
{
    Task<UserWordState?> GetAsync(UserId userId, WordId wordId);
    Task AddOrUpdateAsync(UserWordState state);
}