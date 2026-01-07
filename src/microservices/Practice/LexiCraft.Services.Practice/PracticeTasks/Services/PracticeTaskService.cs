using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// Service for managing practice task persistence and retrieval
/// </summary>
public class PracticeTaskService : IPracticeTaskService
{
    private readonly IPracticeTaskRepository _repository;
    private readonly ILogger<PracticeTaskService> _logger;

    public PracticeTaskService(IPracticeTaskRepository repository, ILogger<PracticeTaskService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<PracticeTask>> SaveTasksAsync(List<PracticeTask> tasks)
    {
        if (!tasks.Any())
        {
            return new List<PracticeTask>();
        }

        try
        {
            _logger.LogInformation("Saving {TaskCount} practice tasks", tasks.Count);

            var savedTasks = new List<PracticeTask>();
            
            foreach (var task in tasks)
            {
                var savedTask = await _repository.InsertAsync(task);
                savedTasks.Add(savedTask);
            }

            _logger.LogInformation("Successfully saved {TaskCount} practice tasks", savedTasks.Count);
            return savedTasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving practice tasks");
            throw;
        }
    }

    public async Task<PracticeTask> SaveTaskAsync(PracticeTask task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        try
        {
            _logger.LogInformation("Saving practice task for user {UserId}, word {WordId}", 
                task.UserId, task.WordId);

            var savedTask = await _repository.InsertAsync(task);

            _logger.LogInformation("Successfully saved practice task {TaskId}", savedTask.Id);
            return savedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving practice task for user {UserId}, word {WordId}", 
                task.UserId, task.WordId);
            throw;
        }
    }

    public async Task<PracticeTask> UpdateTaskStatusAsync(string taskId, PracticeTaskStatus status, Guid userId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new ArgumentException("Task ID cannot be empty", nameof(taskId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        try
        {
            var task = await _repository.GetAsync(t => t.Id == taskId);
            
            if (task == null)
            {
                throw new InvalidOperationException($"Practice task with ID {taskId} not found");
            }

            if (task.UserId != userId)
            {
                throw new UnauthorizedAccessException("User is not authorized to update this task");
            }

            var oldStatus = task.Status;
            task.Status = status;

            // Set completion time if task is being completed
            if (status == PracticeTaskStatus.Completed && oldStatus != PracticeTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }

            var updatedTask = await _repository.UpdateAsync(task);

            _logger.LogInformation("Updated task {TaskId} status from {OldStatus} to {NewStatus} for user {UserId}", 
                taskId, oldStatus, status, userId);

            return updatedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId} status to {Status} for user {UserId}", 
                taskId, status, userId);
            throw;
        }
    }

    public async Task<List<PracticeTask>> GetUserTasksAsync(Guid userId, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        try
        {
            _logger.LogDebug("Getting tasks for user {UserId} with status {Status}", userId, status);

            // Use the paged method with a large page size to get all results
            var (_, tasks) = await _repository.GetUserTasksPagedAsync(userId, 1, int.MaxValue, status, fromDate, toDate);

            _logger.LogDebug("Found {TaskCount} tasks for user {UserId}", tasks.Count, userId);
            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(int total, List<PracticeTask> tasks)> GetUserTasksPagedAsync(Guid userId, int pageIndex, int pageSize, PracticeTaskStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (pageIndex <= 0)
        {
            throw new ArgumentException("Page index must be greater than zero", nameof(pageIndex));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));
        }

        try
        {
            _logger.LogDebug("Getting paged tasks for user {UserId}, page {PageIndex}, size {PageSize}", 
                userId, pageIndex, pageSize);

            var result = await _repository.GetUserTasksPagedAsync(userId, pageIndex, pageSize, status, fromDate, toDate);

            _logger.LogDebug("Found {Total} total tasks, returning {TaskCount} for user {UserId}", 
                result.total, result.tasks.Count, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged tasks for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PracticeTask?> GetTaskByIdAsync(string taskId, Guid userId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new ArgumentException("Task ID cannot be empty", nameof(taskId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        try
        {
            var task = await _repository.GetAsync(t => t.Id == taskId);
            
            if (task == null)
            {
                _logger.LogDebug("Task {TaskId} not found", taskId);
                return null;
            }

            if (task.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access task {TaskId} owned by {TaskUserId}", 
                    userId, taskId, task.UserId);
                return null;
            }

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId} for user {UserId}", taskId, userId);
            throw;
        }
    }

    public async Task<List<PracticeTask>> GetTasksByWordIdsAsync(Guid userId, List<long> wordIds, PracticeTaskStatus? status = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (!wordIds.Any())
        {
            return new List<PracticeTask>();
        }

        try
        {
            _logger.LogDebug("Getting tasks for user {UserId} and {WordCount} words", userId, wordIds.Count);

            var tasks = await _repository.GetTasksByWordIdsAsync(userId, wordIds);

            // Filter by status if provided
            if (status.HasValue)
            {
                tasks = tasks.Where(t => t.Status == status.Value).ToList();
            }

            _logger.LogDebug("Found {TaskCount} tasks for user {UserId} and specified words", tasks.Count, userId);
            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks by word IDs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PracticeTask> CompleteTaskAsync(string taskId, Guid userId)
    {
        return await UpdateTaskStatusAsync(taskId, PracticeTaskStatus.Completed, userId);
    }

    public async Task<PracticeTask> StartTaskAsync(string taskId, Guid userId)
    {
        return await UpdateTaskStatusAsync(taskId, PracticeTaskStatus.InProgress, userId);
    }

    public async Task<PracticeTask> AbandonTaskAsync(string taskId, Guid userId)
    {
        return await UpdateTaskStatusAsync(taskId, PracticeTaskStatus.Abandoned, userId);
    }
}