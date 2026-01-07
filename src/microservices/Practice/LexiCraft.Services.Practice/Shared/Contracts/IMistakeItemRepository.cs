using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IMistakeItemRepository : IRepository<MistakeItem>
{
    Task<List<MistakeItem>> GetUserMistakesAsync(Guid userId, DateTime? fromDate = null);
    Task<List<MistakeItem>> GetMistakesByWordIdsAsync(Guid userId, List<long> wordIds);
    Task<(int total, List<MistakeItem> mistakes)> GetUserMistakesPagedAsync(Guid userId, int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);
    Task<(int total, List<MistakeItem> mistakes)> GetMistakesByWordIdsPagedAsync(Guid userId, List<long> wordIds, int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);
}