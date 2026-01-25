using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.Assessments.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IMistakeItemRepository : IQueryRepository<MistakeItem>
{
    Task<List<MistakeItem>> GetUserMistakesAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<MistakeItem>> GetWordMistakesAsync(string wordId, CancellationToken cancellationToken = default);

    Task<List<MistakeItem>> GetMistakesByTypeAsync(MistakeType mistakeType,
        CancellationToken cancellationToken = default);

    Task<List<MistakeItem>> GetMistakesInDateRangeAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default);
}