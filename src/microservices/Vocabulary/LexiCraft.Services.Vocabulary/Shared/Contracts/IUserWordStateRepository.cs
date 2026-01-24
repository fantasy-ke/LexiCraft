using BuildingBlocks.Domain;
using LexiCraft.Services.Vocabulary.UserStates.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IUserWordStateRepository : IQueryRepository<UserWordState>
{
    Task<UserWordState?> GetAsync(Guid userId, long wordId);
    Task AddOrUpdateAsync(UserWordState state);
}
