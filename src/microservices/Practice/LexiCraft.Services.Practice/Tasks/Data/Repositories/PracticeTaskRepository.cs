using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.MongoDB.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Practice.Tasks.Data.Repositories;

public class PracticeTaskRepository(
    IMongoDbContext context,
    IMongoResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor)
    : MongoRepository<PracticeTask>(context, resilienceService, performanceMonitor, PracticeDbContext.PracticeTasksCollectionName),
        IPracticeTaskRepository
{
    public async Task<PracticeTask?> GetActiveTaskForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return await FirstOrDefaultAsync(
            x => x.UserId == userId && x.Status == PracticeStatus.InProgress,
            cancellationToken);
    }

    public async Task<List<PracticeTask>> GetCompletedTasksAsync(
        UserId userId,
        int limit = 10,
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

    public Task<List<PracticeTask>> GetTasksByTypeAsync(
        UserId userId,
        PracticeTaskType taskType,
        CancellationToken cancellationToken = default)
    {
        return FindAsync(x => x.UserId == userId && x.TaskType == taskType, cancellationToken);
    }

    public Task<List<PracticeTask>> GetTasksBySourceAsync(
        UserId userId,
        PracticeTaskSource sourceType,
        CancellationToken cancellationToken = default)
    {
        return FindAsync(x => x.UserId == userId && x.SourceType == sourceType, cancellationToken);
    }

    public Task<List<PracticeTask>> GetActiveTasksForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return FindAsync(x => x.UserId == userId && x.Status == PracticeStatus.InProgress, cancellationToken);
    }
}
