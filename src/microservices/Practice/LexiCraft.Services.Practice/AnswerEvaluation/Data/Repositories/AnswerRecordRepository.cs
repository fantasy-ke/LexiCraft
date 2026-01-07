using System.Linq.Expressions;
using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Data.Repositories;

public class AnswerRecordRepository(
    PracticeDbContext context,
    IResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor,
    ILogger<AnswerRecordRepository> logger)
    : ResilientMongoRepository<AnswerRecord>(context.Database, resilienceService, performanceMonitor, logger),
        IAnswerRecordRepository
{
    public async Task<List<AnswerRecord>> GetUserAnswersAsync(Guid userId, DateTime? fromDate = null)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetUserAnswers", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var predicate = fromDate.HasValue 
                    ? (Expression<Func<AnswerRecord, bool>>)(x => x.UserId == userId && x.SubmittedAt >= fromDate.Value)
                    : x => x.UserId == userId;

                var (items, _) = await FindPagedAsync(
                    filter: predicate,
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.SubmittedAt,
                    sortDescending: true);

                return items;
            },
            "GetUserAnswers");
    }

    public async Task<List<AnswerRecord>> GetAnswersByTaskIdAsync(string taskId)
    {
        using var monitor = PerformanceMonitor.StartOperation("GetAnswersByTaskId", "answer_records");
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var (items, _) = await FindPagedAsync(
                    filter: x => x.PracticeTaskId == taskId,
                    skip: 0,
                    limit: int.MaxValue,
                    sortBy: x => x.SubmittedAt,
                    sortDescending: true);

                return items;
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
                Expression<Func<AnswerRecord, bool>> predicate = x => x.UserId == userId;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.SubmittedAt >= fromDate.Value && x.SubmittedAt <= toDate.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.SubmittedAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && x.SubmittedAt <= toDate.Value;
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.SubmittedAt,
                    sortDescending: true);

                return ((int)totalCount, items);
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
                Expression<Func<AnswerRecord, bool>> predicate;
                
                if (fromDate.HasValue && toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.SubmittedAt >= fromDate.Value && x.SubmittedAt <= toDate.Value;
                }
                else if (fromDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.SubmittedAt >= fromDate.Value;
                }
                else if (toDate.HasValue)
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId) && 
                                   x.SubmittedAt <= toDate.Value;
                }
                else
                {
                    predicate = x => x.UserId == userId && wordIds.Contains(x.WordId);
                }

                var (items, totalCount) = await FindPagedAsync(
                    filter: predicate,
                    skip: (pageIndex - 1) * pageSize,
                    limit: pageSize,
                    sortBy: x => x.SubmittedAt,
                    sortDescending: true);

                return ((int)totalCount, items);
            },
            "GetAnswersByWordIdsPaged");
    }
}