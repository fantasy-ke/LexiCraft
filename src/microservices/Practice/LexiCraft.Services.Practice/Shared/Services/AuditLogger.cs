using BuildingBlocks.Authentication.Contract;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 练习活动审计日志实现
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly IUserContext _userContext;

    public AuditLogger(ILogger<AuditLogger> logger, IUserContext userContext)
    {
        _logger = logger;
        _userContext = userContext;
    }

    public async Task LogPracticeTaskGenerationAsync(Guid userId, string userName, List<long> wordIds, PracticeType practiceType, int taskCount, bool success, string? errorMessage = null)
    {
        var auditData = new
        {
            EventType = "PracticeTaskGeneration",
            UserId = userId,
            UserName = userName,
            WordIds = wordIds,
            PracticeType = practiceType.ToString(),
            TaskCount = taskCount,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        if (success)
        {
            _logger.LogInformation("Practice task generation completed successfully. {@AuditData}", auditData);
        }
        else
        {
            _logger.LogWarning("Practice task generation failed. {@AuditData}", auditData);
        }

        await Task.CompletedTask;
    }

    public async Task LogAnswerSubmissionAsync(Guid userId, string userName, string practiceTaskId, long wordId, string userAnswer, bool isCorrect, double score, TimeSpan responseTime, bool success, string? errorMessage = null)
    {
        var auditData = new
        {
            EventType = "AnswerSubmission",
            UserId = userId,
            UserName = userName,
            PracticeTaskId = practiceTaskId,
            WordId = wordId,
            UserAnswer = userAnswer,
            IsCorrect = isCorrect,
            Score = score,
            ResponseTimeMs = responseTime.TotalMilliseconds,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        if (success)
        {
            _logger.LogInformation("Answer submission completed successfully. {@AuditData}", auditData);
        }
        else
        {
            _logger.LogWarning("Answer submission failed. {@AuditData}", auditData);
        }

        await Task.CompletedTask;
    }

    public async Task LogPracticeHistoryAccessAsync(Guid userId, string userName, DateTime? fromDate, DateTime? toDate, List<long>? wordIds, int pageIndex, int pageSize, int resultCount, bool success, string? errorMessage = null)
    {
        var auditData = new
        {
            EventType = "PracticeHistoryAccess",
            UserId = userId,
            UserName = userName,
            FromDate = fromDate,
            ToDate = toDate,
            WordIds = wordIds,
            PageIndex = pageIndex,
            PageSize = pageSize,
            ResultCount = resultCount,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        if (success)
        {
            _logger.LogInformation("Practice history access completed successfully. {@AuditData}", auditData);
        }
        else
        {
            _logger.LogWarning("Practice history access failed. {@AuditData}", auditData);
        }

        await Task.CompletedTask;
    }

    public async Task LogAuthenticationEventAsync(string eventType, Guid? userId, string? userName, string ipAddress, string userAgent, bool success, string? errorMessage = null)
    {
        var auditData = new
        {
            EventType = $"Authentication_{eventType}",
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        if (success)
        {
            _logger.LogInformation("Authentication event completed successfully. {@AuditData}", auditData);
        }
        else
        {
            _logger.LogWarning("Authentication event failed. {@AuditData}", auditData);
        }

        await Task.CompletedTask;
    }

    public async Task LogAuthorizationEventAsync(string action, string resource, Guid userId, string userName, string[] permissions, bool success, string? errorMessage = null)
    {
        var auditData = new
        {
            EventType = "Authorization",
            Action = action,
            Resource = resource,
            UserId = userId,
            UserName = userName,
            Permissions = permissions,
            Success = success,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        if (success)
        {
            _logger.LogInformation("Authorization check completed successfully. {@AuditData}", auditData);
        }
        else
        {
            _logger.LogWarning("Authorization check failed. {@AuditData}", auditData);
        }

        await Task.CompletedTask;
    }

    public async Task LogPerformanceDataAsync(Guid userId, string userName, string operation, TimeSpan duration, bool success, Dictionary<string, object>? additionalData = null)
    {
        var auditData = new
        {
            EventType = "Performance",
            UserId = userId,
            UserName = userName,
            Operation = operation,
            DurationMs = duration.TotalMilliseconds,
            Success = success,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        _logger.LogInformation("Performance data recorded. {@AuditData}", auditData);

        await Task.CompletedTask;
    }

    public async Task LogErrorEventAsync(string operation, Guid? userId, string? userName, Exception exception, Dictionary<string, object>? additionalData = null)
    {
        var auditData = new
        {
            EventType = "Error",
            Operation = operation,
            UserId = userId,
            UserName = userName,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow,
            RequestId = GetRequestId()
        };

        _logger.LogError(exception, "Error occurred during operation. {@AuditData}", auditData);

        await Task.CompletedTask;
    }

    private string GetRequestId()
    {
        // Try to get request ID from current context, fallback to new GUID
        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}