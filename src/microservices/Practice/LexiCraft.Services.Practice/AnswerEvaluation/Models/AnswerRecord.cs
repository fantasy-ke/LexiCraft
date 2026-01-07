using BuildingBlocks.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Models;

/// <summary>
/// 表示用户对练习任务的答案及评估结果
/// </summary>
public class AnswerRecord : MongoEntity
{
    /// <summary>
    /// 此答案所属的练习任务ID
    /// </summary>
    [BsonElement("practiceTaskId")]
    [BsonRequired]
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// 提交此答案的用户ID
    /// </summary>
    [BsonElement("userId")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid UserId { get; set; }

    /// <summary>
    /// 正在练习的单词ID
    /// </summary>
    [BsonElement("wordId")]
    public long WordId { get; set; }

    /// <summary>
    /// 用户提供的答案
    /// </summary>
    [BsonElement("userAnswer")]
    [BsonRequired]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 期望的正确答案
    /// </summary>
    [BsonElement("expectedAnswer")]
    [BsonRequired]
    public string ExpectedAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 答案是否完全正确
    /// </summary>
    [BsonElement("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 此答案的得分(0.0到1.0)
    /// </summary>
    [BsonElement("score")]
    public double Score { get; set; }

    /// <summary>
    /// 详细的评估结果，包括错误和反馈
    /// </summary>
    [BsonElement("evaluationResult")]
    public AnswerEvaluationResult EvaluationResult { get; set; } = new();

    /// <summary>
    /// 答案提交时间
    /// </summary>
    [BsonElement("submittedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户响应所花费的时间
    /// </summary>
    [BsonElement("responseTime")]
    [BsonTimeSpanOptions(MongoDB.Bson.BsonType.String)]
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// 详细的评估结果
/// </summary>
public class AnswerEvaluationResult
{
    /// <summary>
    /// 答案是否完全正确
    /// </summary>
    [BsonElement("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 准确度得分，范围从0.0到1.0
    /// </summary>
    [BsonElement("accuracy")]
    public double Accuracy { get; set; }

    /// <summary>
    /// 答案中发现的错误列表
    /// </summary>
    [BsonElement("errors")]
    public List<ErrorDetail> Errors { get; set; } = new();

    /// <summary>
    /// 给用户的反馈信息
    /// </summary>
    [BsonElement("feedback")]
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// 用户答案中特定错误的详细信息
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// 答案中错误的位置
    /// </summary>
    [BsonElement("position")]
    public int Position { get; set; }

    /// <summary>
    /// 此位置应该是什么内容
    /// </summary>
    [BsonElement("expected")]
    public string Expected { get; set; } = string.Empty;

    /// <summary>
    /// 此位置实际提供的是什么内容
    /// </summary>
    [BsonElement("actual")]
    public string Actual { get; set; } = string.Empty;

    /// <summary>
    /// 错误类别
    /// </summary>
    [BsonElement("category")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public ErrorCategory Category { get; set; }
}

/// <summary>
/// 答案中可能出现的错误类别
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// 字符被另一个字符替代
    /// </summary>
    CharacterSubstitution = 1,

    /// <summary>
    /// 插入了额外的字符
    /// </summary>
    CharacterInsertion = 2,

    /// <summary>
    /// 字符被删除或缺失
    /// </summary>
    CharacterDeletion = 3,

    /// <summary>
    /// 字符被交换或转置
    /// </summary>
    CharacterTransposition = 4,

    /// <summary>
    /// 答案完全错误
    /// </summary>
    CompletelyWrong = 5
}