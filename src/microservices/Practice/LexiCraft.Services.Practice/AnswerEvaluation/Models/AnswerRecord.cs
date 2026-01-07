using BuildingBlocks.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Models;

/// <summary>
/// Represents a user's answer to a practice task with evaluation results
/// </summary>
public class AnswerRecord : MongoEntity
{
    /// <summary>
    /// The ID of the practice task this answer belongs to
    /// </summary>
    [BsonElement("practiceTaskId")]
    [BsonRequired]
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who submitted this answer
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the word being practiced
    /// </summary>
    [BsonElement("wordId")]
    public long WordId { get; set; }

    /// <summary>
    /// The answer provided by the user
    /// </summary>
    [BsonElement("userAnswer")]
    [BsonRequired]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// The expected correct answer
    /// </summary>
    [BsonElement("expectedAnswer")]
    [BsonRequired]
    public string ExpectedAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Whether the answer is completely correct
    /// </summary>
    [BsonElement("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// The score for this answer (0.0 to 1.0)
    /// </summary>
    [BsonElement("score")]
    public double Score { get; set; }

    /// <summary>
    /// Detailed evaluation results including errors and feedback
    /// </summary>
    [BsonElement("evaluationResult")]
    public AnswerEvaluationResult EvaluationResult { get; set; } = new();

    /// <summary>
    /// When the answer was submitted
    /// </summary>
    [BsonElement("submittedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How long the user took to respond
    /// </summary>
    [BsonElement("responseTime")]
    [BsonTimeSpanOptions(MongoDB.Bson.BsonType.String)]
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Detailed evaluation results for an answer
/// </summary>
public class AnswerEvaluationResult
{
    /// <summary>
    /// Whether the answer is completely correct
    /// </summary>
    [BsonElement("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Accuracy score from 0.0 to 1.0
    /// </summary>
    [BsonElement("accuracy")]
    public double Accuracy { get; set; }

    /// <summary>
    /// List of errors found in the answer
    /// </summary>
    [BsonElement("errors")]
    public List<ErrorDetail> Errors { get; set; } = new();

    /// <summary>
    /// Feedback message for the user
    /// </summary>
    [BsonElement("feedback")]
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// Details about a specific error in the user's answer
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// Position of the error in the answer
    /// </summary>
    [BsonElement("position")]
    public int Position { get; set; }

    /// <summary>
    /// What was expected at this position
    /// </summary>
    [BsonElement("expected")]
    public string Expected { get; set; } = string.Empty;

    /// <summary>
    /// What was actually provided at this position
    /// </summary>
    [BsonElement("actual")]
    public string Actual { get; set; } = string.Empty;

    /// <summary>
    /// Category of the error
    /// </summary>
    [BsonElement("category")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public ErrorCategory Category { get; set; }
}

/// <summary>
/// Categories of errors that can occur in answers
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// A character was substituted with another
    /// </summary>
    CharacterSubstitution = 1,

    /// <summary>
    /// An extra character was inserted
    /// </summary>
    CharacterInsertion = 2,

    /// <summary>
    /// A character was deleted/missing
    /// </summary>
    CharacterDeletion = 3,

    /// <summary>
    /// Characters were swapped/transposed
    /// </summary>
    CharacterTransposition = 4,

    /// <summary>
    /// The answer is completely wrong
    /// </summary>
    CompletelyWrong = 5
}