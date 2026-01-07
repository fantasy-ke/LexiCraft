using LexiCraft.Services.Practice.AnswerEvaluation.Models;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Services;

/// <summary>
/// Service for evaluating user answers against expected answers
/// </summary>
public interface IAnswerEvaluator
{
    /// <summary>
    /// Evaluates a user's answer against the expected answer
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <returns>Detailed evaluation results including accuracy and feedback</returns>
    Task<AnswerEvaluationResult> EvaluateAnswerAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// Calculates the accuracy score for a user's answer
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <returns>Accuracy score from 0.0 to 1.0</returns>
    Task<double> CalculateAccuracyAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// Generates immediate feedback for the user based on their answer
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <param name="isCorrect">Whether the answer is correct</param>
    /// <param name="accuracy">The accuracy score</param>
    /// <returns>Feedback message for the user</returns>
    Task<string> GenerateFeedbackAsync(string userAnswer, string expectedAnswer, bool isCorrect, double accuracy);
}