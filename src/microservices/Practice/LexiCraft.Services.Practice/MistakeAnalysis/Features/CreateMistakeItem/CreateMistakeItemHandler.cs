using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Features.CreateMistakeItem;

/// <summary>
/// Handler for CreateMistakeItemCommand
/// </summary>
public class CreateMistakeItemHandler : IRequestHandler<CreateMistakeItemCommand, CreateMistakeItemResponse>
{
    private readonly IMistakeItemRepository _mistakeRepository;
    private readonly ILogger<CreateMistakeItemHandler> _logger;

    public CreateMistakeItemHandler(
        IMistakeItemRepository mistakeRepository,
        ILogger<CreateMistakeItemHandler> logger)
    {
        _mistakeRepository = mistakeRepository;
        _logger = logger;
    }

    public async Task<CreateMistakeItemResponse> Handle(CreateMistakeItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating mistake item for user {UserId} on word {WordId}", 
            request.UserId, request.WordId);

        try
        {
            var mistakeItem = new MistakeItem
            {
                AnswerRecordId = request.AnswerRecordId,
                UserId = request.UserId,
                WordId = request.WordId,
                WordSpelling = request.WordSpelling,
                UserAnswer = request.UserAnswer,
                ErrorType = request.ErrorType,
                ErrorDetails = request.ErrorDetails,
                OccurredAt = DateTime.UtcNow
            };

            await _mistakeRepository.InsertAsync(mistakeItem);

            _logger.LogInformation("Successfully created mistake item {MistakeItemId} for user {UserId}",
                mistakeItem.Id, request.UserId);

            return new CreateMistakeItemResponse
            {
                MistakeItem = mistakeItem,
                Success = true
            };
        }
        catch (Exception ex)
        {
            var errorMessage = "Failed to create mistake item";
            _logger.LogError(ex, "Error creating mistake item for user {UserId} on word {WordId}",
                request.UserId, request.WordId);

            return new CreateMistakeItemResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}