using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// Service for managing practice task persistence and retrieval
/// </summary>
public interface IPracticeTaskService
{
    /// <summary>
    /// Saves generated practice tasks to the database
    /// </summary>
    /// <param name="tasks">List of practice tasks to save</param>
    /// <returns>List of saved practice tasks with assigned IDs</returns>
    Task<List<PracticeTask>> SaveTasksAsync(List<PracticeTask> tasks);

    /// <summary>
    /// Saves a single practice task to the database
    /// </summary>
    /// <param name="task">Practice task to save</param>
    /// <returns>Saved practice task with assigned ID</returns>
    Task<PracticeTask> SaveTaskAsync(PracticeTask task);

    /// <summary>
    /// Updates the status of a practice task
    /// </summary>
    /// <param name="taskId">ID of the task to update</param>
    /// <param name="status">New status</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Updated practice task</returns>
    Task<PracticeTask> UpdateTaskStatusAsync(string taskId, PracticeTaskStatus status, Guid userId);

    /// <summary>
    /// Gets practice tasks for a user with optional filtering
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>List of practice tasks</returns>
    Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets practice tasks for a user with pagination
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageIndex">Page index (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>Paged result with total count and tasks</returns>
    Task<(int total, List<PracticeTask> tasks)> GetUserTasksPagedAsync(Guid userId, int pageIndex, int pageSize, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets a practice task by ID
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Practice task or null if not found</returns>
    Task<PracticeTask?> GetTaskByIdAsync(string taskId, Guid userId);

    /// <summary>
    /// Gets practice tasks for specific words
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="wordIds">List of word IDs</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of practice tasks</returns>
    Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds, PracticeTaskStatus? status = null);

    /// <summary>
    /// Marks a task as completed
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Updated practice task</returns>
    Task<PracticeTask> CompleteTaskAsync(string taskId, Guid userId);

    /// <summary>
    /// Marks a task as in progress
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Updated practice task</returns>
    Task<PracticeTask> StartTaskAsync(string taskId, Guid userId);

    /// <summary>
    /// Marks a task as abandoned
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Updated practice task</returns>
    Task<PracticeTask> AbandonTaskAsync(string taskId, Guid userId);
}