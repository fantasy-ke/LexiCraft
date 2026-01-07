using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Data.Repositories;

public class AnswerRecordRepository : PerformantMongoRepository<AnswerRecord>, IAnswerRecordRepository
{
    private readonly PracticeDbContext _context;

    public AnswerRecordRepository(
        PracticeDbContext context,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<AnswerRecordRepository> logger) 
        : base(context, resilienceService, performanceMonitor, logger)
    {
        _context = context;
    }

    public async Task<List<AnswerRecord>> GetUserAnswersAsync(Guid userId, DateTime? fromDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserAnswers", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<AnswerRecord>.Filter.Eq(x => x.UserId, userId);
                
                if (fromDate.HasValue)
                {
                    filter = Builders<AnswerRecord>.Filter.And(filter, 
                        Builders<AnswerRecord>.Filter.Gte(x => x.SubmittedAt, fromDate.Value));
                }

                return await _context.AnswerRecords
                    .Find(filter)
                    .SortByDescending(x => x.SubmittedAt)
                    .ToListAsync();
            },
            "GetUserAnswers");
    }

    public async Task<List<AnswerRecord>> GetAnswersByTaskIdAsync(string taskId)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetAnswersByTaskId", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<AnswerRecord>.Filter.Eq(x => x.PracticeTaskId, taskId);

                return await _context.AnswerRecords
                    .Find(filter)
                    .SortByDescending(x => x.SubmittedAt)
                    .ToListAsync();
            },
            "GetAnswersByTaskId");
    }

    public async Task<(int total, List<AnswerRecord> answers)> GetUserAnswersPagedAsync(
        Guid userId, 
        int pageIndex, 
        int pageSize, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserAnswersPaged", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<AnswerRecord>.Filter.Eq(x => x.UserId, userId);
                
                if (fromDate.HasValue)
                {
                    filter = Builders<AnswerRecord>.Filter.And(filter, 
                        Builders<AnswerRecord>.Filter.Gte(x => x.SubmittedAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<AnswerRecord>.Filter.And(filter,
                        Builders<AnswerRecord>.Filter.Lte(x => x.SubmittedAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.AnswerRecords.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var answersTask = findOptions
                    .SortByDescending(x => x.SubmittedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, answersTask);
                
                return ((int)countTask.Result, answersTask.Result);
            },
            "GetUserAnswersPaged");
    }

    public async Task<(int total, List<AnswerRecord> answers)> GetAnswersByWordIdsPagedAsync(
        Guid userId, 
        List<long> wordIds, 
        int pageIndex, 
        int pageSize, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetAnswersByWordIdsPaged", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<AnswerRecord>.Filter.And(
                    Builders<AnswerRecord>.Filter.Eq(x => x.UserId, userId),
                    Builders<AnswerRecord>.Filter.In(x => x.WordId, wordIds)
                );

                if (fromDate.HasValue)
                {
                    filter = Builders<AnswerRecord>.Filter.And(filter,
                        Builders<AnswerRecord>.Filter.Gte(x => x.SubmittedAt, fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    filter = Builders<AnswerRecord>.Filter.And(filter,
                        Builders<AnswerRecord>.Filter.Lte(x => x.SubmittedAt, toDate.Value));
                }

                // Use parallel execution for better performance
                var findOptions = _context.AnswerRecords.Find(filter);
                var countTask = findOptions.CountDocumentsAsync();
                
                var answersTask = findOptions
                    .SortByDescending(x => x.SubmittedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                await Task.WhenAll(countTask, answersTask);
                
                return ((int)countTask.Result, answersTask.Result);
            },
            "GetAnswersByWordIdsPaged");
    }
}