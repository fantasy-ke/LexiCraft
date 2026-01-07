using BuildingBlocks.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.PracticeTasks.Models;

/// <summary>
/// Represents a practice task for vocabulary learning exercises
/// </summary>
public class PracticeTask : MongoEntity
{
    /// <summary>
    /// The ID of the user who owns this practice task
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
    /// The spelling of the word being practiced
    /// </summary>
    [BsonElement("wordSpelling")]
    [BsonRequired]
    public string WordSpelling { get; set; } = string.Empty;

    /// <summary>
    /// The definition of the word being practiced
    /// </summary>
    [BsonElement("wordDefinition")]
    [BsonRequired]
    public string WordDefinition { get; set; } = string.Empty;

    /// <summary>
    /// The phonetic transcription of the word (optional)
    /// </summary>
    [BsonElement("wordPhonetic")]
    [BsonIgnoreIfNull]
    public string? WordPhonetic { get; set; }

    /// <summary>
    /// URL to the pronunciation audio file (optional)
    /// </summary>
    [BsonElement("pronunciationUrl")]
    [BsonIgnoreIfNull]
    public string? PronunciationUrl { get; set; }

    /// <summary>
    /// The type of practice exercise
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public PracticeType Type { get; set; }

    /// <summary>
    /// The expected correct answer for this practice task
    /// </summary>
    [BsonElement("expectedAnswer")]
    [BsonRequired]
    public string ExpectedAnswer { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the practice task
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public PracticeTaskStatus Status { get; set; } = PracticeTaskStatus.Pending;

    /// <summary>
    /// When the practice task was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the practice task was completed (if applicable)
    /// </summary>
    [BsonElement("completedAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Types of practice exercises available
/// </summary>
public enum PracticeType
{
    /// <summary>
    /// Dictation exercise - listen and write the word (听音写词)
    /// </summary>
    Dictation = 1,

    /// <summary>
    /// Definition to word exercise - read definition and write the word (看义写词)
    /// </summary>
    DefinitionToWord = 2
}

/// <summary>
/// Status of a practice task
/// </summary>
public enum PracticeTaskStatus
{
    /// <summary>
    /// Task has been created but not started
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Task is currently being worked on
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Task has been completed
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Task was started but abandoned
    /// </summary>
    Abandoned = 4
}