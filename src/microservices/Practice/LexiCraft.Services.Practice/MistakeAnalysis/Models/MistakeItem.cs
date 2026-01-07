using BuildingBlocks.MongoDB;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.MistakeAnalysis.Models;

/// <summary>
/// 表示用户在练习中犯的错误，包含错误分类
/// </summary>
public class MistakeItem : MongoEntity
{
    /// <summary>
    /// 此错误所属的答案记录ID
    /// </summary>
    [BsonElement("answerRecordId")]
    [BsonRequired]
    public string AnswerRecordId { get; set; } = string.Empty;

    /// <summary>
    /// 犯此错误的用户ID
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid UserId { get; set; }

    /// <summary>
    /// 回答错误的单词ID
    /// </summary>
    [BsonElement("wordId")]
    public long WordId { get; set; }

    /// <summary>
    /// 单词的正确拼写
    /// </summary>
    [BsonElement("wordSpelling")]
    [BsonRequired]
    public string WordSpelling { get; set; } = string.Empty;

    /// <summary>
    /// 用户提供的错误答案
    /// </summary>
    [BsonElement("userAnswer")]
    [BsonRequired]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 错误类型（拼写错误vs完全错误）
    /// </summary>
    [BsonElement("errorType")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public ErrorType ErrorType { get; set; }

    /// <summary>
    /// 发现的错误详细分解
    /// </summary>
    [BsonElement("errorDetails")]
    public List<ErrorDetail> ErrorDetails { get; set; } = new();

    /// <summary>
    /// 此错误发生的时间
    /// </summary>
    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 可以分类的错误类型
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// 轻微拼写错误 - 接近正确答案
    /// </summary>
    SpellingError = 1,

    /// <summary>
    /// 完全错误 - 答案完全错误
    /// </summary>
    CompleteError = 2
}