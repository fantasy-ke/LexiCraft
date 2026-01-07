using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IPracticeTaskRepository : IRepository<PracticeTask>
{
    Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null);
    Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds);
    Task<(int total, List<PracticeTask> tasks)> GetUserTasksPagedAsync(Guid userId, int pageIndex, int pageSize, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<(int total, List<PracticeTask> tasks)> GetTasksByWordIdsPagedAsync(Guid userId, List<long> wordIds, int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);
}