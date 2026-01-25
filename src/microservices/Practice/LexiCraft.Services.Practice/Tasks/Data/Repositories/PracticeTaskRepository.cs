using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Tasks.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Tasks.Data.Repositories;

public class PracticeTaskRepository(
    IMongoDatabase database,
    IResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor,
    ILogger<PracticeTaskRepository> logger)
    : ResilientMongoRepository<PracticeTask>(database, resilienceService, performanceMonitor, logger, "practice_tasks"),
        IPracticeTaskRepository
{
    public async Task<PracticeTask?> GetActiveTaskForUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await FirstOrDefaultAsync(x => x.UserId == userId && x.Status == PracticeStatus.InProgress);
    }

    public async Task<List<PracticeTask>> GetCompletedTasksAsync(string userId, int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await FindPagedAsync(
            x => x.UserId == userId && x.Status == PracticeStatus.Finished,
            0,
            limit,
            x => x.FinishedAt!,
            true,
            cancellationToken);

        return items;
    }

    public async Task<List<PracticeTask>> GetTasksByTypeAsync(string userId, PracticeTaskType taskType,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.UserId == userId && x.TaskType == taskType, cancellationToken);
    }

    public async Task<List<PracticeTask>> GetTasksBySourceAsync(string userId, PracticeTaskSource sourceType,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.UserId == userId && x.SourceType == sourceType, cancellationToken);
    }

    public async Task<List<PracticeTask>> GetActiveTasksForUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.UserId == userId && x.Status == PracticeStatus.InProgress, cancellationToken);
    }
}