using BuildingBlocks.Authentication.Contract;
using LexiCraft.Services.Practice.PracticeTasks.Services;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;

/// <summary>
/// Handler for GeneratePracticeTasksCommand
/// </summary>
public class GeneratePracticeTasksHandler : IRequestHandler<GeneratePracticeTasksCommand, GeneratePracticeTasksResponse>
{
    private readonly IPracticeTaskGenerator _taskGenerator;
    private readonly IPracticeTaskRepository _taskRepository;
    private readonly ILogger<GeneratePracticeTasksHandler> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly IUserContext _userContext;

    public GeneratePracticeTasksHandler(
        IPracticeTaskGenerator taskGenerator,
        IPracticeTaskRepository taskRepository,
        ILogger<GeneratePracticeTasksHandler> logger,
        IAuditLogger auditLogger,
        IUserContext userContext)
    {
        _taskGenerator = taskGenerator;
        _taskRepository = taskRepository;
        _logger = logger;
        _auditLogger = auditLogger;
        _userContext = userContext;
    }

    public async Task<GeneratePracticeTasksResponse> Handle(GeneratePracticeTasksCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        string? errorMessage = null;

        try
        {
            _logger.LogInformation("Generating practice tasks for user {UserId} with {WordCount} words, type: {PracticeType}",
                request.UserId, request.WordIds.Count, request.PracticeType);

            // Determine how many tasks to generate
            var wordIdsToUse = request.Count.HasValue && request.Count.Value < request.WordIds.Count
                ? request.WordIds.Take(request.Count.Value).ToList()
                : request.WordIds;

            // Generate practice tasks
            var tasks = await _taskGenerator.GenerateTasksAsync(
                request.UserId,
                wordIdsToUse,
                request.PracticeType,
                wordIdsToUse.Count);

            if (!tasks.Any())
            {
                errorMessage = "No tasks could be generated for the specified words";
                _logger.LogWarning("No tasks were generated for user {UserId}", request.UserId);
                
                await _auditLogger.LogPracticeTaskGenerationAsync(
                    request.UserId,
                    _userContext.UserName,
                    request.WordIds,
                    request.PracticeType,
                    0,
                    false,
                    errorMessage);

                return new GeneratePracticeTasksResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Save tasks to repository
            foreach (var task in tasks)
            {
                await _taskRepository.InsertAsync(task);
            }

            success = true;
            _logger.LogInformation("Successfully generated {TaskCount} practice tasks for user {UserId}",
                tasks.Count, request.UserId);

            // Log audit information
            await _auditLogger.LogPracticeTaskGenerationAsync(
                request.UserId,
                _userContext.UserName,
                request.WordIds,
                request.PracticeType,
                tasks.Count,
                true);

            // Log performance data
            var duration = DateTime.UtcNow - startTime;
            await _auditLogger.LogPerformanceDataAsync(
                request.UserId,
                _userContext.UserName,
                "GeneratePracticeTasks",
                duration,
                true,
                new Dictionary<string, object>
                {
                    ["WordCount"] = request.WordIds.Count,
                    ["PracticeType"] = request.PracticeType.ToString(),
                    ["TasksGenerated"] = tasks.Count
                });

            return new GeneratePracticeTasksResponse
            {
                Tasks = tasks,
                TotalGenerated = tasks.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while generating practice tasks";
            _logger.LogError(ex, "Error generating practice tasks for user {UserId}", request.UserId);
            
            // Log audit information for failure
            await _auditLogger.LogPracticeTaskGenerationAsync(
                request.UserId,
                _userContext.UserName,
                request.WordIds,
                request.PracticeType,
                0,
                false,
                errorMessage);

            // Log error event
            await _auditLogger.LogErrorEventAsync(
                "GeneratePracticeTasks",
                request.UserId,
                _userContext.UserName,
                ex,
                new Dictionary<string, object>
                {
                    ["WordIds"] = request.WordIds,
                    ["PracticeType"] = request.PracticeType.ToString()
                });

            return new GeneratePracticeTasksResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}