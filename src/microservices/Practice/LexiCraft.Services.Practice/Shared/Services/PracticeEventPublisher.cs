using BuildingBlocks.EventBus.Abstractions;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Events;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 具有弹性模式的练习相关事件发布服务
/// </summary>
public class PracticeEventPublisher : IPracticeEventPublisher
{
    private readonly IEventBus<PracticeCompletionEvent> _practiceCompletionEventBus;
    private readonly IEventBus<MistakeIdentifiedEvent> _mistakeEventBus;
    private readonly IEventBus<PerformanceDataEvent> _performanceEventBus;
    private readonly ILogger<PracticeEventPublisher> _logger;
    private readonly SemaphoreSlim _circuitBreakerSemaphore;
    private DateTime _circuitBreakerOpenedAt = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private const int MaxConsecutiveFailures = 5;
    private static readonly TimeSpan CircuitBreakerTimeout = TimeSpan.FromMinutes(1);

    public PracticeEventPublisher(
        IEventBus<PracticeCompletionEvent> practiceCompletionEventBus,
        IEventBus<MistakeIdentifiedEvent> mistakeEventBus,
        IEventBus<PerformanceDataEvent> performanceEventBus,
        ILogger<PracticeEventPublisher> logger)
    {
        _practiceCompletionEventBus = practiceCompletionEventBus;
        _mistakeEventBus = mistakeEventBus;
        _performanceEventBus = performanceEventBus;
        _logger = logger;
        _circuitBreakerSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task PublishPracticeCompletionAsync(PracticeTask practiceTask, AnswerRecord answerRecord)
    {
        var completionEvent = new PracticeCompletionEvent
        {
            UserId = practiceTask.UserId,
            PracticeTaskId = practiceTask.Id,
            WordId = practiceTask.WordId,
            WordSpelling = practiceTask.WordSpelling,
            IsCorrect = answerRecord.IsCorrect,
            Score = answerRecord.Score,
            ResponseTime = answerRecord.ResponseTime,
            CompletedAt = answerRecord.SubmittedAt,
            PracticeType = practiceTask.Type.ToString()
        };

        await PublishEventSafelyAsync(
            async () => await _practiceCompletionEventBus.PublishAsync(completionEvent).AsTask(),
            "PracticeCompletion",
            practiceTask.UserId,
            practiceTask.Id);
    }

    public async Task PublishMistakeIdentifiedAsync(MistakeItem mistakeItem, PracticeTask practiceTask)
    {
        var mistakeEvent = new MistakeIdentifiedEvent
        {
            UserId = mistakeItem.UserId,
            WordId = mistakeItem.WordId,
            WordSpelling = mistakeItem.WordSpelling,
            UserAnswer = mistakeItem.UserAnswer,
            ExpectedAnswer = practiceTask.ExpectedAnswer,
            ErrorType = mistakeItem.ErrorType,
            ErrorDetails = mistakeItem.ErrorDetails,
            OccurredAt = mistakeItem.OccurredAt,
            PracticeType = practiceTask.Type.ToString()
        };

        await PublishEventSafelyAsync(
            async () => await _mistakeEventBus.PublishAsync(mistakeEvent).AsTask(),
            "MistakeIdentified",
            mistakeItem.UserId,
            mistakeItem.Id);
    }

    public async Task PublishPerformanceDataAsync(Guid userId, List<AnswerRecord> answerRecords, List<PracticeTask> practiceTasks)
    {
        if (!answerRecords.Any())
        {
            _logger.LogWarning("No answer records provided for performance data event for user {UserId}", userId);
            return;
        }

        var taskPerformances = answerRecords.Select(ar =>
        {
            var task = practiceTasks.FirstOrDefault(pt => pt.Id == ar.PracticeTaskId);
            return new TaskPerformance
            {
                TaskId = ar.PracticeTaskId,
                WordId = ar.WordId,
                WordSpelling = task?.WordSpelling ?? string.Empty,
                IsCorrect = ar.IsCorrect,
                Score = ar.Score,
                ResponseTime = ar.ResponseTime,
                PracticeType = task?.Type.ToString() ?? string.Empty,
                CompletedAt = ar.SubmittedAt
            };
        }).ToList();

        var sessionStartTime = answerRecords.Min(ar => ar.SubmittedAt.Subtract(ar.ResponseTime));
        var sessionEndTime = answerRecords.Max(ar => ar.SubmittedAt);
        var correctAnswers = answerRecords.Count(ar => ar.IsCorrect);
        var totalTasks = answerRecords.Count;
        var overallAccuracy = totalTasks > 0 ? (double)correctAnswers / totalTasks : 0.0;
        var totalResponseTime = TimeSpan.FromMilliseconds(answerRecords.Sum(ar => ar.ResponseTime.TotalMilliseconds));
        var averageResponseTime = totalTasks > 0 
            ? TimeSpan.FromMilliseconds(totalResponseTime.TotalMilliseconds / totalTasks) 
            : TimeSpan.Zero;

        var performanceEvent = new PerformanceDataEvent
        {
            UserId = userId,
            SessionId = Guid.NewGuid().ToString(), // 基于批次生成会话ID
            TaskPerformances = taskPerformances,
            SessionStartTime = sessionStartTime,
            SessionEndTime = sessionEndTime,
            OverallAccuracy = overallAccuracy,
            TotalTasks = totalTasks,
            CorrectAnswers = correctAnswers,
            IncorrectAnswers = totalTasks - correctAnswers,
            TotalResponseTime = totalResponseTime,
            AverageResponseTime = averageResponseTime
        };

        await PublishEventSafelyAsync(
            async () => await _performanceEventBus.PublishAsync(performanceEvent).AsTask(),
            "PerformanceData",
            userId,
            performanceEvent.SessionId);
    }

    /// <summary>
    /// 使用弹性模式发布事件，确保用户体验不受故障影响
    /// </summary>
    private async Task PublishEventSafelyAsync(Func<Task> publishAction, string eventType, Guid userId, string entityId)
    {
        // 检查熔断器状态
        if (await IsCircuitBreakerOpenAsync())
        {
            _logger.LogWarning("Circuit breaker is open, skipping {EventType} event publication for user {UserId}, entity {EntityId}", 
                eventType, userId, entityId);
            return;
        }

        // 使用指数退避重试
        var maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await publishAction();
                
                // 成功时重置失败计数
                await ResetCircuitBreakerAsync();
                
                _logger.LogDebug("Successfully published {EventType} event for user {UserId}, entity {EntityId}", 
                    eventType, userId, entityId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event publishing attempt {Attempt}/{MaxRetries} failed for {EventType} event, user {UserId}, entity {EntityId}", 
                    attempt, maxRetries, eventType, userId, entityId);

                if (attempt == maxRetries)
                {
                    // 所有重试都失败 - 更新熔断器并记录错误
                    await RecordFailureAsync();
                    _logger.LogError(ex, "Failed to publish {EventType} event for user {UserId}, entity {EntityId} after all retry attempts", 
                        eventType, userId, entityId);
                    return;
                }

                // 指数退避延迟
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
        }
    }

    private async Task<bool> IsCircuitBreakerOpenAsync()
    {
        await _circuitBreakerSemaphore.WaitAsync();
        try
        {
            if (_consecutiveFailures >= MaxConsecutiveFailures)
            {
                if (DateTime.UtcNow - _circuitBreakerOpenedAt < CircuitBreakerTimeout)
                {
                    return true; // 熔断器仍然开启
                }
                else
                {
                    // 熔断器超时已过期，重置为半开状态
                    _consecutiveFailures = 0;
                    _logger.LogInformation("Event publishing circuit breaker reset after timeout");
                    return false;
                }
            }
            return false;
        }
        finally
        {
            _circuitBreakerSemaphore.Release();
        }
    }

    private async Task RecordFailureAsync()
    {
        await _circuitBreakerSemaphore.WaitAsync();
        try
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= MaxConsecutiveFailures)
            {
                _circuitBreakerOpenedAt = DateTime.UtcNow;
                _logger.LogError("Event publishing circuit breaker opened after {FailureCount} consecutive failures", _consecutiveFailures);
            }
        }
        finally
        {
            _circuitBreakerSemaphore.Release();
        }
    }

    private async Task ResetCircuitBreakerAsync()
    {
        await _circuitBreakerSemaphore.WaitAsync();
        try
        {
            if (_consecutiveFailures > 0)
            {
                _consecutiveFailures = 0;
                _logger.LogDebug("Event publishing circuit breaker reset after successful operation");
            }
        }
        finally
        {
            _circuitBreakerSemaphore.Release();
        }
    }
}