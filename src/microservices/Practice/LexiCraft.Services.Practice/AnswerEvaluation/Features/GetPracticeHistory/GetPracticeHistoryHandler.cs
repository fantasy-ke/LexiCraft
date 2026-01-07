using BuildingBlocks.Authentication.Contract;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;

/// <summary>
/// Handler for GetPracticeHistoryQuery
/// </summary>
public class GetPracticeHistoryHandler : IRequestHandler<GetPracticeHistoryQuery, GetPracticeHistoryResponse>
{
    private readonly IAnswerRecordRepository _answerRepository;
    private readonly ILogger<GetPracticeHistoryHandler> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly IUserContext _userContext;

    public GetPracticeHistoryHandler(
        IAnswerRecordRepository answerRepository,
        ILogger<GetPracticeHistoryHandler> logger,
        IAuditLogger auditLogger,
        IUserContext userContext)
    {
        _answerRepository = answerRepository;
        _logger = logger;
        _auditLogger = auditLogger;
        _userContext = userContext;
    }

    public async Task<GetPracticeHistoryResponse> Handle(GetPracticeHistoryQuery request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        string? errorMessage = null;
        var resultCount = 0;

        try
        {
            _logger.LogInformation("Retrieving practice history for user {UserId}, page {PageIndex}, size {PageSize}",
                request.UserId, request.PageIndex, request.PageSize);

            // Get paginated results based on filters
            var (totalCount, answerRecords) = request.WordIds?.Any() == true
                ? await _answerRepository.GetAnswersByWordIdsPagedAsync(
                    request.UserId,
                    request.WordIds,
                    request.PageIndex,
                    request.PageSize,
                    request.FromDate,
                    request.ToDate)
                : await _answerRepository.GetUserAnswersPagedAsync(
                    request.UserId,
                    request.PageIndex,
                    request.PageSize,
                    request.FromDate,
                    request.ToDate);

            resultCount = answerRecords.Count;

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var hasNextPage = request.PageIndex < totalPages - 1;
            var hasPreviousPage = request.PageIndex > 0;

            success = true;
            _logger.LogInformation("Retrieved {RecordCount} practice history records for user {UserId} (total: {TotalCount})",
                answerRecords.Count, request.UserId, totalCount);

            // Log audit information
            await _auditLogger.LogPracticeHistoryAccessAsync(
                request.UserId,
                _userContext.UserName,
                request.FromDate,
                request.ToDate,
                request.WordIds,
                request.PageIndex,
                request.PageSize,
                resultCount,
                true);

            // Log performance data
            var duration = DateTime.UtcNow - startTime;
            await _auditLogger.LogPerformanceDataAsync(
                request.UserId,
                _userContext.UserName,
                "GetPracticeHistory",
                duration,
                true,
                new Dictionary<string, object>
                {
                    ["PageIndex"] = request.PageIndex,
                    ["PageSize"] = request.PageSize,
                    ["ResultCount"] = resultCount,
                    ["TotalCount"] = totalCount,
                    ["HasFilters"] = request.WordIds?.Any() == true || request.FromDate.HasValue || request.ToDate.HasValue
                });

            return new GetPracticeHistoryResponse
            {
                AnswerRecords = answerRecords,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                Success = true
            };
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while retrieving practice history";
            _logger.LogError(ex, "Error retrieving practice history for user {UserId}", request.UserId);

            // Log audit information for failure
            await _auditLogger.LogPracticeHistoryAccessAsync(
                request.UserId,
                _userContext.UserName,
                request.FromDate,
                request.ToDate,
                request.WordIds,
                request.PageIndex,
                request.PageSize,
                0,
                false,
                errorMessage);

            // Log error event
            await _auditLogger.LogErrorEventAsync(
                "GetPracticeHistory",
                request.UserId,
                _userContext.UserName,
                ex,
                new Dictionary<string, object>
                {
                    ["PageIndex"] = request.PageIndex,
                    ["PageSize"] = request.PageSize,
                    ["FromDate"] = request.FromDate?.ToString() ?? "null",
                    ["ToDate"] = request.ToDate?.ToString() ?? "null",
                    ["WordIds"] = request.WordIds?.ToArray() ?? Array.Empty<long>()
                });

            return new GetPracticeHistoryResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}