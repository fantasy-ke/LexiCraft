using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.PracticeTasks.Data.Repositories;

public class PracticeTaskRepository : PerformantMongoRepository<PracticeTask>, IPracticeTaskRepository
{
    private readonly PracticeDbContext _context;

    public PracticeTaskRepository(
        PracticeDbContext context,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<PracticeTaskRepository> logger) 
        : base(context, resilienceService, performanceMonitor, logger)
    {
        _context = context;
    }

    public async Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserTasks", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<PracticeTask>.Filter.Eq(x => x.UserId, userId);
                
                if (status.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter, 
                        Builders<PracticeTask>.Filter.Eq(x => x.Status, status.Value));
                }

                return await _context.PracticeTasks
                    .Find(filter)
                    .SortByDescending(x => x.CreatedAt)
                    .ToListAsync();
            },
            "GetUserTasks");
    }

    public async Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetTasksByWordIds", "practice_tasks");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<PracticeTask>.Filter.And(
                    Builders<PracticeTask>.Filter.Eq(x => x.UserId, userId),
                    Builders<PracticeTask>.Filter.In(x => x.WordId, wordIds)
                );

                return await _context.PracticeTasks
                    .Find(filter)
                    .SortByDescending(x => x.CreatedAt)
                    .ToListAsync();
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
                var filter = Builders<PracticeTask>.Filter.Eq(x => x.UserId, userId);
                
                if (status.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter, 
                        Builders<PracticeTask>.Filter.Eq(x => x.Status, status.Value));
                }

                if (fromDate.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter,
                        Builders<PracticeTask>.Filter.Gte(x => x.CreatedAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter,
                        Builders<PracticeTask>.Filter.Lte(x => x.CreatedAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.PracticeTasks.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var tasksTask = findOptions
                    .SortByDescending(x => x.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, tasksTask);
                
                return ((int)countTask.Result, tasksTask.Result);
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
                var filter = Builders<PracticeTask>.Filter.And(
                    Builders<PracticeTask>.Filter.Eq(x => x.UserId, userId),
                    Builders<PracticeTask>.Filter.In(x => x.WordId, wordIds)
                );

                if (fromDate.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter,
                        Builders<PracticeTask>.Filter.Gte(x => x.CreatedAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<PracticeTask>.Filter.And(filter,
                        Builders<PracticeTask>.Filter.Lte(x => x.CreatedAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.PracticeTasks.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var tasksTask = findOptions
                    .SortByDescending(x => x.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, tasksTask);
                
                return ((int)countTask.Result, tasksTask.Result);
            },
            "GetTasksByWordIdsPaged");
    }
}