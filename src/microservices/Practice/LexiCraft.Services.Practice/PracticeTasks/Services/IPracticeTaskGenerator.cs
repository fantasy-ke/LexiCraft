using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// 用于生成词汇学习练习任务的服务
/// </summary>
public interface IPracticeTaskGenerator
{
    /// <summary>
    /// 根据单词ID为用户生成多个练习任务
    /// </summary>
    /// <param name="userId">用户的ID</param>
    /// <param name="wordIds">要生成任务的单词ID列表</param>
    /// <param name="type">练习类型</param>
    /// <param name="count">要生成的最大任务数量</param>
    /// <returns>生成的练习任务列表</returns>
    Task<List<PracticeTask>> GenerateTasksAsync(Guid userId, List<long> wordIds, PracticeType type, int count);

    /// <summary>
    /// 为特定单词生成单个练习任务
    /// </summary>
    /// <param name="userId">用户的ID</param>
    /// <param name="wordId">要生成任务的单词ID</param>
    /// <param name="type">练习类型</param>
    /// <returns>生成的练习任务</returns>
    Task<PracticeTask> GenerateSingleTaskAsync(Guid userId, long wordId, PracticeType type);
}