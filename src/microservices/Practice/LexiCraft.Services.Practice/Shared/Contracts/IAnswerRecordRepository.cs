using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.Assessments.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IAnswerRecordRepository : IQueryRepository<AnswerRecord>
{
    Task<List<AnswerRecord>> GetTaskAnswersAsync(Guid practiceTaskItemId, CancellationToken cancellationToken = default);
    Task<AnswerRecord?> GetUserAnswerAsync(Guid practiceTaskItemId, string userId, CancellationToken cancellationToken = default);
    Task<List<AnswerRecord>> GetAnswersByStatusAsync(AnswerStatus status, CancellationToken cancellationToken = default);
    Task<List<AnswerRecord>> GetAnswersInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}