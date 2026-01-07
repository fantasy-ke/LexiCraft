using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Services;

/// <summary>
/// Service for classifying and analyzing errors in user answers
/// </summary>
public class ErrorClassifier : IErrorClassifier
{
    private readonly ILogger<ErrorClassifier> _logger;
    
    // Threshold for determining spelling error vs complete error
    private const double SpellingErrorThreshold = 0.4;

    public ErrorClassifier(ILogger<ErrorClassifier> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorType> ClassifyErrorAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return ErrorType.CompleteError;
        }

        try
        {
            _logger.LogDebug("Classifying error for '{UserAnswer}' vs '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);

            // Calculate accuracy to determine error type
            var accuracy = CalculateAccuracy(userAnswer, expectedAnswer);
            var isSpellingError = await IsSpellingErrorAsync(userAnswer, expectedAnswer, accuracy);

            var errorType = isSpellingError ? ErrorType.SpellingError : ErrorType.CompleteError;

            _logger.LogDebug("Classified as {ErrorType} (accuracy: {Accuracy})", errorType, accuracy);

            return errorType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying error for '{UserAnswer}' vs '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);
            return ErrorType.CompleteError;
        }
    }

    public async Task<List<ErrorDetail>> AnalyzeErrorsAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return new List<ErrorDetail>
            {
                new ErrorDetail
                {
                    Position = 0,
                    Expected = expectedAnswer ?? "",
                    Actual = userAnswer ?? "",
                    Category = ErrorCategory.CompletelyWrong
                }
            };
        }

        try
        {
            _logger.LogDebug("Analyzing errors for '{UserAnswer}' vs '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);

            // Normalize for comparison
            var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
            var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

            // If completely different, return single complete error
            var accuracy = CalculateAccuracy(userAnswer, expectedAnswer);
            if (accuracy < SpellingErrorThreshold)
            {
                return new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Position = 0,
                        Expected = expectedAnswer,
                        Actual = userAnswer,
                        Category = ErrorCategory.CompletelyWrong
                    }
                };
            }

            // Perform character-level analysis
            var errors = await PerformCharacterLevelAnalysisAsync(normalizedUserAnswer, normalizedExpectedAnswer);

            _logger.LogDebug("Found {ErrorCount} character-level errors", errors.Count);

            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing errors for '{UserAnswer}' vs '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);
            return new List<ErrorDetail>();
        }
    }

    public async Task<MistakeItem> CreateMistakeItemAsync(string answerRecordId, Guid userId, long wordId, string wordSpelling, string userAnswer)
    {
        if (string.IsNullOrEmpty(answerRecordId))
        {
            throw new ArgumentException("Answer record ID cannot be null or empty", nameof(answerRecordId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (string.IsNullOrEmpty(wordSpelling))
        {
            throw new ArgumentException("Word spelling cannot be null or empty", nameof(wordSpelling));
        }

        try
        {
            _logger.LogDebug("Creating mistake item for user {UserId}, word {WordId}", userId, wordId);

            var errorType = await ClassifyErrorAsync(userAnswer, wordSpelling);
            var errorDetails = await AnalyzeErrorsAsync(userAnswer, wordSpelling);

            var mistakeItem = new MistakeItem
            {
                AnswerRecordId = answerRecordId,
                UserId = userId,
                WordId = wordId,
                WordSpelling = wordSpelling,
                UserAnswer = userAnswer ?? "",
                ErrorType = errorType,
                ErrorDetails = errorDetails,
                OccurredAt = DateTime.UtcNow
            };

            _logger.LogDebug("Created mistake item with {ErrorCount} error details", errorDetails.Count);

            return mistakeItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mistake item for user {UserId}, word {WordId}", userId, wordId);
            throw;
        }
    }

    public async Task<bool> IsSpellingErrorAsync(string userAnswer, string expectedAnswer, double accuracy)
    {
        try
        {
            // If accuracy is above threshold, consider it a spelling error
            // If below threshold, consider it a complete error
            var isSpellingError = accuracy >= SpellingErrorThreshold;

            _logger.LogDebug("Accuracy {Accuracy} vs threshold {Threshold}: {IsSpellingError}", 
                accuracy, SpellingErrorThreshold, isSpellingError ? "Spelling Error" : "Complete Error");

            return await Task.FromResult(isSpellingError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining if spelling error");
            return false;
        }
    }

    /// <summary>
    /// Performs character-level analysis to identify specific errors
    /// </summary>
    private async Task<List<ErrorDetail>> PerformCharacterLevelAnalysisAsync(string userAnswer, string expectedAnswer)
    {
        var errors = new List<ErrorDetail>();

        try
        {
            // Use dynamic programming to find the optimal alignment
            var alignment = FindOptimalAlignment(userAnswer, expectedAnswer);

            // Analyze the alignment to identify specific errors
            for (int i = 0; i < alignment.Count; i++)
            {
                var (userChar, expectedChar, position) = alignment[i];

                if (userChar != expectedChar)
                {
                    var category = DetermineErrorCategory(userChar, expectedChar, i, alignment);
                    
                    errors.Add(new ErrorDetail
                    {
                        Position = position,
                        Expected = expectedChar?.ToString() ?? "",
                        Actual = userChar?.ToString() ?? "",
                        Category = category
                    });
                }
            }

            return await Task.FromResult(errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in character-level analysis");
            return new List<ErrorDetail>();
        }
    }

    /// <summary>
    /// Finds the optimal character alignment between two strings
    /// </summary>
    private List<(char? userChar, char? expectedChar, int position)> FindOptimalAlignment(string userAnswer, string expectedAnswer)
    {
        var alignment = new List<(char? userChar, char? expectedChar, int position)>();

        var userLength = userAnswer.Length;
        var expectedLength = expectedAnswer.Length;

        // Simple alignment - can be improved with more sophisticated algorithms
        var maxLength = Math.Max(userLength, expectedLength);

        for (int i = 0; i < maxLength; i++)
        {
            char? userChar = i < userLength ? userAnswer[i] : null;
            char? expectedChar = i < expectedLength ? expectedAnswer[i] : null;

            alignment.Add((userChar, expectedChar, i));
        }

        return alignment;
    }

    /// <summary>
    /// Determines the category of error based on character differences
    /// </summary>
    private ErrorCategory DetermineErrorCategory(char? userChar, char? expectedChar, int position, 
        List<(char? userChar, char? expectedChar, int position)> alignment)
    {
        // Character deletion (expected character missing)
        if (userChar == null && expectedChar != null)
        {
            return ErrorCategory.CharacterDeletion;
        }

        // Character insertion (extra character in user answer)
        if (userChar != null && expectedChar == null)
        {
            return ErrorCategory.CharacterInsertion;
        }

        // Character substitution (different characters)
        if (userChar != null && expectedChar != null && userChar != expectedChar)
        {
            // Check for transposition (characters swapped)
            if (position + 1 < alignment.Count)
            {
                var nextAlignment = alignment[position + 1];
                if (nextAlignment.userChar == expectedChar && nextAlignment.expectedChar == userChar)
                {
                    return ErrorCategory.CharacterTransposition;
                }
            }

            return ErrorCategory.CharacterSubstitution;
        }

        return ErrorCategory.CharacterSubstitution;
    }

    /// <summary>
    /// Calculates accuracy using Levenshtein distance
    /// </summary>
    private double CalculateAccuracy(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return 0.0;
        }

        var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
        var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

        if (normalizedUserAnswer == normalizedExpectedAnswer)
        {
            return 1.0;
        }

        var distance = CalculateLevenshteinDistance(normalizedUserAnswer, normalizedExpectedAnswer);
        var maxLength = Math.Max(normalizedUserAnswer.Length, normalizedExpectedAnswer.Length);

        return maxLength == 0 ? 0.0 : Math.Max(0.0, 1.0 - (double)distance / maxLength);
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings
    /// </summary>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var matrix = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (var i = 0; i <= sourceLength; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            matrix[0, j] = j;
        }

        // Fill the matrix
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,     // deletion
                        matrix[i, j - 1] + 1),    // insertion
                    matrix[i - 1, j - 1] + cost  // substitution
                );
            }
        }

        return matrix[sourceLength, targetLength];
    }
}