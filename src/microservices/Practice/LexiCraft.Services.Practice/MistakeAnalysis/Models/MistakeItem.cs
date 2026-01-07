using BuildingBlocks.MongoDB;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Models;

/// <summary>
/// Represents a mistake made by a user during practice with error classification
/// </summary>
public class MistakeItem : MongoEntity
{
    /// <summary>
    /// The ID of the answer record this mistake belongs to
    /// </summary>
    [BsonElement("answerRecordId")]
    [BsonRequired]
    public string AnswerRecordId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who made this mistake
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid UserId { get; set; }

    /// <summary>
    /// The ID of the word that was answered incorrectly
    /// </summary>
    [BsonElement("wordId")]
    public long WordId { get; set; }

    /// <summary>
    /// The correct spelling of the word
    /// </summary>
    [BsonElement("wordSpelling")]
    [BsonRequired]
    public string WordSpelling { get; set; } = string.Empty;

    /// <summary>
    /// The incorrect answer provided by the user
    /// </summary>
    [BsonElement("userAnswer")]
    [BsonRequired]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// The type of error made (spelling vs complete error)
    /// </summary>
    [BsonElement("errorType")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public ErrorType ErrorType { get; set; }

    /// <summary>
    /// Detailed breakdown of the errors found
    /// </summary>
    [BsonElement("errorDetails")]
    public List<ErrorDetail> ErrorDetails { get; set; } = new();

    /// <summary>
    /// When this mistake occurred
    /// </summary>
    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of errors that can be classified
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Minor spelling error - close to correct answer (拼写错误)
    /// </summary>
    SpellingError = 1,

    /// <summary>
    /// Complete error - answer is completely wrong (完全错误)
    /// </summary>
    CompleteError = 2
}