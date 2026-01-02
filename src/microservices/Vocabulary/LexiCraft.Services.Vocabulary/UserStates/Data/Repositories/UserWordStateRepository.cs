using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.UserStates.Data.Repositories;

public class UserWordStateRepository(VocabularyDbContext context) : IUserWordStateRepository
{
    public async Task<UserWordState?> GetAsync(Guid userId, long wordId)
    {
        return await context.UserWordStates.FirstOrDefaultAsync(x => x.UserId == userId && x.WordId == wordId);
    }

    public IQueryable<UserWordState> Query()
    {
        return context.UserWordStates.AsQueryable();
    }

    public async Task AddOrUpdateAsync(UserWordState state)
    {
        var existing = await GetAsync(state.UserId, state.WordId);
        if (existing == null)
        {
            await context.UserWordStates.AddAsync(state);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(state);
        }
        await context.SaveChangesAsync();
    }
}
