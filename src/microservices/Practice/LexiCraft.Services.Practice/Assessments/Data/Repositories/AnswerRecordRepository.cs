using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using LexiCraft.Services.Practice.Assessments.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Assessments.Data.Repositories;

public class AnswerRecordRepository(
    IMongoDatabase database,
    IResilienceService resilienceService,
    IMongoPerformanceMonitor performanceMonitor,
    ILogger<AnswerRecordRepository> logger)
    : ResilientMongoQueryRepository<AnswerRecord>(database, resilienceService, performanceMonitor, logger,
            "answer_records"),
        IAnswerRecordRepository
{
    public async Task<List<AnswerRecord>> GetTaskAnswersAsync(Guid practiceTaskItemId,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.PracticeTaskItemId == practiceTaskItemId, cancellationToken);
    }

    public async Task<AnswerRecord?> GetUserAnswerAsync(Guid practiceTaskItemId, string userId,
        CancellationToken cancellationToken = default)
    {
        // Note: This assumes we have a way to correlate with user, might need to join with PracticeTask
        // For now, we'll just get by PracticeTaskItemId
        return await FirstOrDefaultAsync(x => x.PracticeTaskItemId == practiceTaskItemId);
    }

    public async Task<List<AnswerRecord>> GetAnswersByStatusAsync(AnswerStatus status,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.Status == status, cancellationToken);
    }

    public async Task<List<AnswerRecord>> GetAnswersInDateRangeAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await FindAsync(x => x.SubmittedAt >= startDate && x.SubmittedAt <= endDate, cancellationToken);
    }
}