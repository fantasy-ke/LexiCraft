using System.Linq.Expressions;
using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.PracticeTasks.Data.Repositories;

public class PracticeTaskRepository(
    PracticeDbContext context,
    IResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor,
    ILogger<PracticeTaskRepository> logger)
    : ResilientMongoRepository<PracticeTask>(context.Database, resilienceService, performanceMonitor, logger),
        IPracticeTaskRepository
{
    public async Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserTasks", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var predicate = status.HasValue 
                    ? (Expression<Func<PracticeTask, bool>>)(x => x.UserId == userId && x.Status == status.Value)
                    : x => x.UserId == userId;

                var (items, _) = await FindPagedAsync(
                    filter: predicate,
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.CreatedAt,
                    sortDescending: true);

                return items;
            },
            "GetUserTasks");
    }

    public async Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetTasksByWordIds", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var (items, _) = await FindPagedAsync(
                    filter: x => x.UserId == userId && wordIds.Contains(x.WordId),
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.CreatedAt,
                    sortDescending: true);

                return items;
            },
            "GetTasksByWordIds");
    }

    public async Task<(int total, List<PracticeTask> tasks)> GetUserTasksPagedAsync(
        Guid userId, 
        int pageIndex, 
        int pageSize, 
        PracticeTaskStatus? status = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserTasksPaged", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                Expression<Func<PracticeTask, bool>> predicate = x => x.UserId == userId;
                
                if (status.HasValue && fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.Status == status.Value && 
                                   x.CreatedAt >= fromDate.Value && x.CreatedAt <= toDate.Value;
                }
                else if (status.HasValue && fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.Status == status.Value && x.CreatedAt >= fromDate.Value;
                }
                else if (status.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.Status == status.Value && x.CreatedAt <= toDate.Value;
                }
                else if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.CreatedAt >= fromDate.Value && x.CreatedAt <= toDate.Value;
                }
                else if (status.HasValue)
                {
                    predicate = x => x.UserId == userId && x.Status == status.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.CreatedAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.CreatedAt <= toDate.Value;
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.CreatedAt,
                    sortDescending: true);

                return ((int)totalCount, items);
            },
            "GetUserTasksPaged");
    }

    public async Task<(int total, List<PracticeTask> tasks)> GetTasksByWordIdsPagedAsync(
        Guid userId, 
        List<long> wordIds, 
        int pageIndex, 
        int pageSize, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetTasksByWordIdsPaged", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                Expression<Func<PracticeTask, bool>> predicate;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.CreatedAt >= fromDate.Value && x.CreatedAt <= toDate.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.CreatedAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.CreatedAt <= toDate.Value;
                }
                else
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId);
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.CreatedAt,
                    sortDescending: true);

                return ((int)totalCount, items);
            },
            "GetTasksByWordIdsPaged");
    }
}