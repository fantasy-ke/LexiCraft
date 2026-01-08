using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Assessments.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Assessments.Data.Repositories;

public class MistakeItemRepository : ResilientMongoRepository<MistakeItem>, IMistakeItemRepository
{
    public MistakeItemRepository(
        IMongoDatabase database,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<MistakeItemRepository> logger)
        : base(database, resilienceService, performanceMonitor, logger, "mistake_items")
    {
    }

    public async Task<List<MistakeItem>> GetUserMistakesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<MistakeItem>> GetWordMistakesAsync(string wordId, CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.WordId == wordId, cancellationToken);
    }

    public async Task<List<MistakeItem>> GetMistakesByTypeAsync(MistakeType mistakeType, CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.MistakeType == mistakeType, cancellationToken);
    }

    public async Task<List<MistakeItem>> GetMistakesInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.OccurredAt >= startDate && x.OccurredAt <= endDate, cancellationToken);
    }
}