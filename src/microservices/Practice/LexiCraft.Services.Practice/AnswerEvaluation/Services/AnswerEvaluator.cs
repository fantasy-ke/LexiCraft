using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Services;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Services;

/// <summary>
/// Service for evaluating user answers against expected answers
/// </summary>
public class AnswerEvaluator : IAnswerEvaluator
{
    private readonly IErrorClassifier _errorClassifier;
    private readonly ILogger<AnswerEvaluator> _logger;

    public AnswerEvaluator(IErrorClassifier errorClassifier, ILogger<AnswerEvaluator> logger)
    {
        _errorClassifier = errorClassifier;
        _logger = logger;
    }

    public async Task<AnswerEvaluationResult> EvaluateAnswerAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer))
        {
            throw new ArgumentException("User answer cannot be null or empty", nameof(userAnswer));
        }

        if (string.IsNullOrEmpty(expectedAnswer))
        {
            throw new ArgumentException("Expected answer cannot be null or empty", nameof(expectedAnswer));
        }

        try
        {
            _logger.LogDebug("Evaluating answer: '{UserAnswer}' against expected: '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);

            // Normalize answers for comparison (trim whitespace, convert to lowercase)
            var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
            var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

            // Check if answer is completely correct
            var isCorrect = normalizedUserAnswer == normalizedExpectedAnswer;

            // Calculate accuracy score
            var accuracy = await CalculateAccuracyAsync(userAnswer, expectedAnswer);

            // Analyze errors if not completely correct
            var errors = new List<ErrorDetail>();
            if (!isCorrect)
            {
                errors = await _errorClassifier.AnalyzeErrorsAsync(userAnswer, expectedAnswer);
            }

            // Generate feedback
            var feedback = await GenerateFeedbackAsync(userAnswer, expectedAnswer, isCorrect, accuracy);

            var result = new AnswerEvaluationResult
            {
                IsCorrect = isCorrect,
                Accuracy = accuracy,
                Errors = errors,
                Feedback = feedback
            };

            _logger.LogDebug("Evaluation complete: IsCorrect={IsCorrect}, Accuracy={Accuracy}, ErrorCount={ErrorCount}", 
                isCorrect, accuracy, errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating answer '{UserAnswer}' against '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);
            throw;
        }
    }

    public async Task<double> CalculateAccuracyAsync(string userAnswer, string expectedAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(expectedAnswer))
        {
            return 0.0;
        }

        try
        {
            // Normalize answers for comparison
            var normalizedUserAnswer = userAnswer.Trim().ToLowerInvariant();
            var normalizedExpectedAnswer = expectedAnswer.Trim().ToLowerInvariant();

            // If exactly correct, return 1.0
            if (normalizedUserAnswer == normalizedExpectedAnswer)
            {
                return 1.0;
            }

            // Calculate similarity using Levenshtein distance
            var distance = CalculateLevenshteinDistance(normalizedUserAnswer, normalizedExpectedAnswer);
            var maxLength = Math.Max(normalizedUserAnswer.Length, normalizedExpectedAnswer.Length);

            // Convert distance to accuracy (0.0 to 1.0)
            var accuracy = maxLength == 0 ? 0.0 : Math.Max(0.0, 1.0 - (double)distance / maxLength);

            _logger.LogDebug("Calculated accuracy: {Accuracy} (distance: {Distance}, maxLength: {MaxLength})", 
                accuracy, distance, maxLength);

            return await Task.FromResult(accuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating accuracy for '{UserAnswer}' vs '{ExpectedAnswer}'", 
                userAnswer, expectedAnswer);
            return 0.0;
        }
    }

    public async Task<string> GenerateFeedbackAsync(string userAnswer, string expectedAnswer, bool isCorrect, double accuracy)
    {
        try
        {
            if (isCorrect)
            {
                return await Task.FromResult("Correct! Well done!");
            }

            if (accuracy >= 0.8)
            {
                return await Task.FromResult($"Very close! The correct answer is '{expectedAnswer}'. You had a minor spelling error.");
            }

            if (accuracy >= 0.5)
            {
                return await Task.FromResult($"Good attempt! The correct answer is '{expectedAnswer}'. Check your spelling.");
            }

            return await Task.FromResult($"The correct answer is '{expectedAnswer}'. Keep practicing!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating feedback");
            return "Answer evaluated. Keep practicing!";
        }
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>The minimum number of single-character edits required to change one string into the other</returns>
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