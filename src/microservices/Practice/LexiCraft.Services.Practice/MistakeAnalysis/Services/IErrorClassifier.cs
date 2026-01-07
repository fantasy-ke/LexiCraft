using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Services;

/// <summary>
/// Service for classifying and analyzing errors in user answers
/// </summary>
public interface IErrorClassifier
{
    /// <summary>
    /// Classifies the type of error made by the user
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <returns>The type of error (spelling error vs complete error)</returns>
    Task<ErrorType> ClassifyErrorAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// Analyzes the specific errors in the user's answer
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <returns>List of detailed error information including position and category</returns>
    Task<List<ErrorDetail>> AnalyzeErrorsAsync(string userAnswer, string expectedAnswer);

    /// <summary>
    /// Creates a mistake item for an incorrect answer
    /// </summary>
    /// <param name="answerRecordId">The ID of the answer record</param>
    /// <param name="userId">The user who made the mistake</param>
    /// <param name="wordId">The word that was answered incorrectly</param>
    /// <param name="wordSpelling">The correct spelling of the word</param>
    /// <param name="userAnswer">The incorrect answer provided</param>
    /// <returns>A mistake item with error classification and details</returns>
    Task<MistakeItem> CreateMistakeItemAsync(string answerRecordId, Guid userId, long wordId, string wordSpelling, string userAnswer);

    /// <summary>
    /// Determines if an error should be classified as a spelling error or complete error
    /// </summary>
    /// <param name="userAnswer">The answer provided by the user</param>
    /// <param name="expectedAnswer">The correct answer</param>
    /// <param name="accuracy">The calculated accuracy score</param>
    /// <returns>True if it's a spelling error, false if it's a complete error</returns>
    Task<bool> IsSpellingErrorAsync(string userAnswer, string expectedAnswer, double accuracy);
}