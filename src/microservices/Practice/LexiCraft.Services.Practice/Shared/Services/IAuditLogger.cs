using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.PracticeTasks.Models;

namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 练习活动审计日志接口
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// 记录练习任务生成活动
    /// </summary>
    Task LogPracticeTaskGenerationAsync(Guid userId, string userName, List<long> wordIds, PracticeType practiceType, int taskCount, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录答案提交活动
    /// </summary>
    Task LogAnswerSubmissionAsync(Guid userId, string userName, string practiceTaskId, long wordId, string userAnswer, bool isCorrect, double score, TimeSpan responseTime, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录练习历史访问
    /// </summary>
    Task LogPracticeHistoryAccessAsync(Guid userId, string userName, DateTime? fromDate, DateTime? toDate, List<long>? wordIds, int pageIndex, int pageSize, int resultCount, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录身份验证事件
    /// </summary>
    Task LogAuthenticationEventAsync(string eventType, Guid? userId, string? userName, string ipAddress, string userAgent, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录授权事件
    /// </summary>
    Task LogAuthorizationEventAsync(string action, string resource, Guid userId, string userName, string[] permissions, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录性能数据
    /// </summary>
    Task LogPerformanceDataAsync(Guid userId, string userName, string operation, TimeSpan duration, bool success, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// 记录错误事件
    /// </summary>
    Task LogErrorEventAsync(string operation, Guid? userId, string? userName, Exception exception, Dictionary<string, object>? additionalData = null);
}