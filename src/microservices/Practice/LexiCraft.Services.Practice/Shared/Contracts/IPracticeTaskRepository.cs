using BuildingBlocks.Domain;
using LexiCraft.Services.Practice.Tasks.Models;

namespace LexiCraft.Services.Practice.Shared.Contracts;

public interface IPracticeTaskRepository : IRepository<PracticeTask>
{
    /// <summary>
    /// Get active practice task for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active practice task or null if none exists</returns>
    Task<PracticeTask?> GetActiveTaskForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get completed practice tasks for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="limit">Maximum number of tasks to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of completed practice tasks</returns>
    Task<List<PracticeTask>> GetCompletedTasksAsync(string userId, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get practice tasks by type for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="taskType">Type of practice task</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of practice tasks of the specified type</returns>
    Task<List<PracticeTask>> GetTasksByTypeAsync(string userId, PracticeTaskType taskType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get practice tasks by source for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="sourceType">Source type of practice task</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of practice tasks from the specified source</returns>
    Task<List<PracticeTask>> GetTasksBySourceAsync(string userId, PracticeTaskSource sourceType, CancellationToken cancellationToken = default);
}