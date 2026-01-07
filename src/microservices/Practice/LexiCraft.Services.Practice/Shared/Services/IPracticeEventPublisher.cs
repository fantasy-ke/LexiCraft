using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 练习相关事件发布服务
/// </summary>
public interface IPracticeEventPublisher
{
    /// <summary>
    /// 发布练习完成事件
    /// </summary>
    Task PublishPracticeCompletionAsync(PracticeTask practiceTask, AnswerRecord answerRecord);

    /// <summary>
    /// 发布错误识别事件
    /// </summary>
    Task PublishMistakeIdentifiedAsync(MistakeItem mistakeItem, PracticeTask practiceTask);

    /// <summary>
    /// 发布练习会话的性能数据
    /// </summary>
    Task PublishPerformanceDataAsync(Guid userId, List<AnswerRecord> answerRecords, List<PracticeTask> practiceTasks);
}