using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IAnswerRecordRepository : IRepository<AnswerRecord>
{
    Task<List<AnswerRecord>> GetUserAnswersAsync(Guid userId, DateTime? fromDate = null);
    Task<List<AnswerRecord>> GetAnswersByTaskIdAsync(string taskId);
    Task<(int total, List<AnswerRecord> answers)> GetUserAnswersPagedAsync(Guid userId, int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);
    Task<(int total, List<AnswerRecord> answers)> GetAnswersByWordIdsPagedAsync(Guid userId, List<long> wordIds, int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);
}