using BuildingBlocks.Authentication.Contract;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.AnswerEvaluation.Services;
using LexiCraft.Services.Practice.MistakeAnalysis.Features.ClassifyError;
using LexiCraft.Services.Practice.MistakeAnalysis.Features.CreateMistakeItem;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using LexiCraft.Services.Practice.Shared.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;

/// <summary>
/// Handler for SubmitAnswerCommand
/// </summary>
public class SubmitAnswerHandler : IRequestHandler<SubmitAnswerCommand, SubmitAnswerResponse>
{
    private readonly IPracticeTaskRepository _taskRepository;
    private readonly IAnswerRecordRepository _answerRepository;
    private readonly IAnswerEvaluator _answerEvaluator;
    private readonly IPracticeEventPublisher _eventPublisher;
    private readonly ILogger<SubmitAnswerHandler> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public SubmitAnswerHandler(
        IPracticeTaskRepository taskRepository,
        IAnswerRecordRepository answerRepository,
        IAnswerEvaluator answerEvaluator,
        IPracticeEventPublisher eventPublisher,
        ILogger<SubmitAnswerHandler> logger,
        IAuditLogger auditLogger,
        IUserContext userContext,
        IMediator mediator)
    {
        _taskRepository = taskRepository;
        _answerRepository = answerRepository;
        _answerEvaluator = answerEvaluator;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _auditLogger = auditLogger;
        _userContext = userContext;
        _mediator = mediator;
    }

    public async Task<SubmitAnswerResponse> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        string? errorMessage = null;
        var responseTime = TimeSpan.FromMilliseconds(request.ResponseTimeMs);

        try
        {
            _logger.LogInformation("Processing answer submission for task {TaskId} by user {UserId}",
                request.PracticeTaskId, request.UserId);

            // Get the practice task
            var practiceTask = await _taskRepository.FirstOrDefaultAsync(t => t.Id == request.PracticeTaskId);
            if (practiceTask == null)
            {
                errorMessage = "Practice task not found";
                _logger.LogWarning("Practice task {TaskId} not found", request.PracticeTaskId);
                
                await _auditLogger.LogAnswerSubmissionAsync(
                    request.UserId,
                    _userContext.UserName,
                    request.PracticeTaskId,
                    0, // Unknown word ID
                    request.UserAnswer,
                    false,
                    0.0,
                    responseTime,
                    false,
                    errorMessage);

                return new SubmitAnswerResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Verify the task belongs to the user
            if (practiceTask.UserId != request.UserId)
            {
                errorMessage = "You are not authorized to submit an answer for this task";
                _logger.LogWarning("User {UserId} attempted to submit answer for task {TaskId} owned by {OwnerId}",
                    request.UserId, request.PracticeTaskId, practiceTask.UserId);
                
                await _auditLogger.LogAnswerSubmissionAsync(
                    request.UserId,
                    _userContext.UserName,
                    request.PracticeTaskId,
                    practiceTask.WordId,
                    request.UserAnswer,
                    false,
                    0.0,
                    responseTime,
                    false,
                    errorMessage);

                return new SubmitAnswerResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Evaluate the answer
            var evaluationResult = await _answerEvaluator.EvaluateAnswerAsync(
                request.UserAnswer, practiceTask.ExpectedAnswer);

            // Create answer record
            var answerRecord = new AnswerRecord
            {
                PracticeTaskId = request.PracticeTaskId,
                UserId = request.UserId,
                WordId = practiceTask.WordId,
                UserAnswer = request.UserAnswer,
                ExpectedAnswer = practiceTask.ExpectedAnswer,
                IsCorrect = evaluationResult.IsCorrect,
                Score = evaluationResult.Accuracy,
                EvaluationResult = evaluationResult,
                SubmittedAt = DateTime.UtcNow,
                ResponseTime = responseTime
            };

            // Save answer record
            await _answerRepository.InsertAsync(answerRecord);

            // Update practice task status
            practiceTask.Status = PracticeTaskStatus.Completed;
            practiceTask.CompletedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(practiceTask);

            MistakeItem? mistakeItem = null;

            // Create mistake item if answer is incorrect using CQRS
            if (!evaluationResult.IsCorrect)
            {
                // 使用 CQRS 分类错误
                var classifyErrorCommand = new ClassifyErrorCommand
                {
                    UserAnswer = request.UserAnswer,
                    ExpectedAnswer = practiceTask.ExpectedAnswer
                };

                var errorClassification = await _mediator.Send(classifyErrorCommand, cancellationToken);

                // 使用 CQRS 创建错题项
                var createMistakeCommand = new CreateMistakeItemCommand
                {
                    AnswerRecordId = answerRecord.Id,
                    UserId = request.UserId,
                    WordId = practiceTask.WordId,
                    WordSpelling = practiceTask.WordSpelling,
                    UserAnswer = request.UserAnswer,
                    ErrorType = errorClassification.ErrorType,
                    ErrorDetails = errorClassification.ErrorDetails
                };

                var mistakeResult = await _mediator.Send(createMistakeCommand, cancellationToken);
                
                if (mistakeResult.Success)
                {
                    mistakeItem = mistakeResult.MistakeItem;
                    _logger.LogInformation("Created mistake item for incorrect answer by user {UserId} on word {WordId}",
                        request.UserId, practiceTask.WordId);
                }
                else
                {
                    _logger.LogWarning("Failed to create mistake item: {ErrorMessage}", mistakeResult.ErrorMessage);
                }
            }

            success = true;
            _logger.LogInformation("Successfully processed answer submission for task {TaskId}. Correct: {IsCorrect}, Score: {Score}",
                request.PracticeTaskId, evaluationResult.IsCorrect, evaluationResult.Accuracy);

            // Log audit information
            await _auditLogger.LogAnswerSubmissionAsync(
                request.UserId,
                _userContext.UserName,
                request.PracticeTaskId,
                practiceTask.WordId,
                request.UserAnswer,
                evaluationResult.IsCorrect,
                evaluationResult.Accuracy,
                responseTime,
                true);

            // Log performance data
            var duration = DateTime.UtcNow - startTime;
            await _auditLogger.LogPerformanceDataAsync(
                request.UserId,
                _userContext.UserName,
                "SubmitAnswer",
                duration,
                true,
                new Dictionary<string, object>
                {
                    ["WordId"] = practiceTask.WordId,
                    ["IsCorrect"] = evaluationResult.IsCorrect,
                    ["Score"] = evaluationResult.Accuracy,
                    ["ResponseTimeMs"] = responseTime.TotalMilliseconds
                });

            // Publish practice completion event
            await _eventPublisher.PublishPracticeCompletionAsync(practiceTask, answerRecord);

            // Publish mistake identified event if answer is incorrect
            if (mistakeItem != null)
            {
                await _eventPublisher.PublishMistakeIdentifiedAsync(mistakeItem, practiceTask);
            }

            return new SubmitAnswerResponse
            {
                AnswerRecord = answerRecord,
                MistakeItem = mistakeItem,
                IsCorrect = evaluationResult.IsCorrect,
                Accuracy = evaluationResult.Accuracy,
                Feedback = evaluationResult.Feedback,
                Success = true
            };
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred while processing your answer";
            _logger.LogError(ex, "Error processing answer submission for task {TaskId} by user {UserId}",
                request.PracticeTaskId, request.UserId);

            // Log audit information for failure
            await _auditLogger.LogAnswerSubmissionAsync(
                request.UserId,
                _userContext.UserName,
                request.PracticeTaskId,
                0, // Unknown word ID in case of error
                request.UserAnswer,
                false,
                0.0,
                responseTime,
                false,
                errorMessage);

            // Log error event
            await _auditLogger.LogErrorEventAsync(
                "SubmitAnswer",
                request.UserId,
                _userContext.UserName,
                ex,
                new Dictionary<string, object>
                {
                    ["PracticeTaskId"] = request.PracticeTaskId,
                    ["UserAnswer"] = request.UserAnswer
                });

            return new SubmitAnswerResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}