using LexiCraft.Services.Vocabulary.UserStates.Models;

namespace LexiCraft.Services.Vocabulary.Shared.Contracts;

public interface IUserWordStateRepository
{
    Task<UserWordState?> GetAsync(Guid userId, long wordId);
    IQueryable<UserWordState> Query();
    Task AddOrUpdateAsync(UserWordState state);
}
