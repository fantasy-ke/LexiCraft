using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// Service for generating practice tasks for vocabulary learning
/// </summary>
public interface IPracticeTaskGenerator
{
    /// <summary>
    /// Generates multiple practice tasks for a user based on word IDs
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="wordIds">List of word IDs to generate tasks for</param>
    /// <param name="type">Type of practice exercise</param>
    /// <param name="count">Maximum number of tasks to generate</param>
    /// <returns>List of generated practice tasks</returns>
    Task<List<PracticeTask>> GenerateTasksAsync(Guid userId, List<long> wordIds, PracticeType type, int count);

    /// <summary>
    /// Generates a single practice task for a specific word
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="wordId">The word ID to generate a task for</param>
    /// <param name="type">Type of practice exercise</param>
    /// <returns>Generated practice task</returns>
    Task<PracticeTask> GenerateSingleTaskAsync(Guid userId, long wordId, PracticeType type);
}