using LexiCraft.Services.Practice.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 性能数据聚合和发布处理服务
/// </summary>
public class PerformanceDataService : IPerformanceDataService
{
    private readonly IAnswerRecordRepository _answerRepository;
    private readonly IPracticeTaskRepository _taskRepository;
    private readonly IPracticeEventPublisher _eventPublisher;
    private readonly ILogger<PerformanceDataService> _logger;

    public PerformanceDataService(
        IAnswerRecordRepository answerRepository,
        IPracticeTaskRepository taskRepository,
        IPracticeEventPublisher eventPublisher,
        ILogger<PerformanceDataService> logger)
    {
        _answerRepository = answerRepository;
        _taskRepository = taskRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task PublishRecentPerformanceDataAsync(Guid userId, DateTime? fromDate = null)
    {
        try
        {
            // 如果未指定日期，默认为最近24小时
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-1);

            _logger.LogInformation("Publishing performance data for user {UserId} from {StartDate}", userId, startDate);

            // 获取最近的答案记录
            var answerRecords = await _answerRepository.GetUserAnswersAsync(userId, startDate);

            if (!answerRecords.Any())
            {
                _logger.LogDebug("No recent answer records found for user {UserId} from {StartDate}", userId, startDate);
                return;
            }

            // 获取对应的练习任务
            var taskIds = answerRecords.Select(ar => ar.PracticeTaskId).Distinct().ToList();
            var practiceTasks = new List<PracticeTasks.Models.PracticeTask>();

            foreach (var taskId in taskIds)
            {
                var task = await _taskRepository.FirstOrDefaultAsync(t => t.Id == taskId);
                if (task != null)
                {
                    practiceTasks.Add(task);
                }
            }

            // 发布性能数据事件
            await _eventPublisher.PublishPerformanceDataAsync(userId, answerRecords, practiceTasks);

            _logger.LogInformation("Successfully published performance data for user {UserId} with {RecordCount} records", 
                userId, answerRecords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing performance data for user {UserId}", userId);
            // 不重新抛出异常 - 这是后台操作，不应影响用户体验
        }
    }
}