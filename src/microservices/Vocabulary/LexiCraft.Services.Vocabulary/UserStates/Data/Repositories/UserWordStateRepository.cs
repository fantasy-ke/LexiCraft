using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.Shared.Data;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Vocabulary.UserStates.Data.Repositories;

public class UserWordStateRepository(VocabularyDbContext context) 
    : Repository<VocabularyDbContext, UserWordState>(context), IUserWordStateRepository
{
    public async Task<UserWordState?> GetAsync(Guid userId, long wordId)
    {
        return await FirstOrDefaultAsync(x => x.UserId == userId && x.WordId == wordId);
    }

    public async Task AddOrUpdateAsync(UserWordState state)
    {
        var existing = await GetAsync(state.UserId, state.WordId);
        if (existing == null)
        {
            await InsertAsync(state);
        }
        else
        {
            DbContext.Entry(existing).CurrentValues.SetValues(state);
        }
        await SaveChangesAsync();
    }
}
