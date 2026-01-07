using System.Linq.Expressions;
using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Data.Repositories;

public class MistakeItemRepository(
    PracticeDbContext context,
    IResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor,
    ILogger<MistakeItemRepository> logger)
    : ResilientMongoRepository<MistakeItem>(context.Database, resilienceService, performanceMonitor, logger),
        IMistakeItemRepository
{
    public async Task<List<MistakeItem>> GetUserMistakesAsync(Guid userId, DateTime? fromDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserMistakes", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var predicate = fromDate.HasValue 
                    ? (Expression<Func<MistakeItem, bool>>)(x => x.UserId == userId && x.OccurredAt >= fromDate.Value)
                    : x => x.UserId == userId;

                var (items, _) = await FindPagedAsync(
                    filter: predicate,
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.OccurredAt,
                    sortDescending: true);

                return items;
            },
            "GetUserMistakes");
    }

    public async Task<List<MistakeItem>> GetMistakesByWordIdsAsync(Guid userId, List<long> wordIds)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetMistakesByWordIds", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var (items, _) = await FindPagedAsync(
                    filter: x => x.UserId == userId && wordIds.Contains(x.WordId),
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.OccurredAt,
                    sortDescending: true);

                return items;
            },
            "GetMistakesByWordIds");
    }

    public async Task<(int total, List<MistakeItem> mistakes)> GetUserMistakesPagedAsync(
        Guid userId, 
        int pageIndex, 
        int pageSize, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserMistakesPaged", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                Expression<Func<MistakeItem, bool>> predicate = x => x.UserId == userId;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.OccurredAt >= fromDate.Value && x.OccurredAt <= toDate.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.OccurredAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.OccurredAt <= toDate.Value;
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.OccurredAt,
                    sortDescending: true);

                return ((int)totalCount, items);
            },
            "GetUserMistakesPaged");
    }

    public async Task<(int total, List<MistakeItem> mistakes)> GetMistakesByWordIdsPagedAsync(
        Guid userId, 
        List<long> wordIds, 
        int pageIndex, 
        int pageSize, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetMistakesByWordIdsPaged", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                Expression<Func<MistakeItem, bool>> predicate;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.OccurredAt >= fromDate.Value && x.OccurredAt <= toDate.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.OccurredAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.OccurredAt <= toDate.Value;
                }
                else
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId);
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.OccurredAt,
                    sortDescending: true);

                return ((int)totalCount, items);
            },
            "GetMistakesByWordIdsPaged");
    }
}