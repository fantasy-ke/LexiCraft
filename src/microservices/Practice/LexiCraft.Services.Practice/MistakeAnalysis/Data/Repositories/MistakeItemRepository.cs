using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Data.Repositories;

public class MistakeItemRepository : PerformantMongoRepository<MistakeItem>, IMistakeItemRepository
{
    private readonly PracticeDbContext _context;

    public MistakeItemRepository(
        PracticeDbContext context,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<MistakeItemRepository> logger) 
        : base(context, resilienceService, performanceMonitor, logger)
    {
        _context = context;
    }

    public async Task<List<MistakeItem>> GetUserMistakesAsync(Guid userId, DateTime? fromDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserMistakes", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<MistakeItem>.Filter.Eq(x => x.UserId, userId);
                
                if (fromDate.HasValue)
                {
                    filter = Builders<MistakeItem>.Filter.And(filter, 
                        Builders<MistakeItem>.Filter.Gte(x => x.OccurredAt, fromDate.Value));
                }

                return await _context.MistakeItems
                    .Find(filter)
                    .SortByDescending(x => x.OccurredAt)
                    .ToListAsync();
            },
            "GetUserMistakes");
    }

    public async Task<List<MistakeItem>> GetMistakesByWordIdsAsync(Guid userId, List<long> wordIds)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetMistakesByWordIds", "mistake_items");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<MistakeItem>.Filter.And(
                    Builders<MistakeItem>.Filter.Eq(x => x.UserId, userId),
                    Builders<MistakeItem>.Filter.In(x => x.WordId, wordIds)
                );

                return await _context.MistakeItems
                    .Find(filter)
                    .SortByDescending(x => x.OccurredAt)
                    .ToListAsync();
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
                var filter = Builders<MistakeItem>.Filter.Eq(x => x.UserId, userId);
                
                if (fromDate.HasValue)
                {
                    filter = Builders<MistakeItem>.Filter.And(filter, 
                        Builders<MistakeItem>.Filter.Gte(x => x.OccurredAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<MistakeItem>.Filter.And(filter,
                        Builders<MistakeItem>.Filter.Lte(x => x.OccurredAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.MistakeItems.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var mistakesTask = findOptions
                    .SortByDescending(x => x.OccurredAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, mistakesTask);
                
                return ((int)countTask.Result, mistakesTask.Result);
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
                var filter = Builders<MistakeItem>.Filter.And(
                    Builders<MistakeItem>.Filter.Eq(x => x.UserId, userId),
                    Builders<MistakeItem>.Filter.In(x => x.WordId, wordIds)
                );

                if (fromDate.HasValue)
                {
                    filter = Builders<MistakeItem>.Filter.And(filter,
                        Builders<MistakeItem>.Filter.Gte(x => x.OccurredAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<MistakeItem>.Filter.And(filter,
                        Builders<MistakeItem>.Filter.Lte(x => x.OccurredAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.MistakeItems.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var mistakesTask = findOptions
                    .SortByDescending(x => x.OccurredAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, mistakesTask);
                
                return ((int)countTask.Result, mistakesTask.Result);
            },
            "GetMistakesByWordIdsPaged");
    }
}