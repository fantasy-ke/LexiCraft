using BuildingBlocks.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace LexiCraft.Services.Practice.PracticeTasks.Models;

/// <summary>
/// 表示词汇学习练习的练习任务
/// </summary>
public class PracticeTask : MongoEntity
{
    /// <summary>
    /// 拥有此练习任务的用户ID
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
    /// 正在练习的单词拼写
    /// </summary>
    [BsonElement("wordSpelling")]
    [BsonRequired]
    public string WordSpelling { get; set; } = string.Empty;

    /// <summary>
    /// 正在练习的单词定义
    /// </summary>
    [BsonElement("wordDefinition")]
    [BsonRequired]
    public string WordDefinition { get; set; } = string.Empty;

    /// <summary>
    /// 单词的音标（可选）
    /// </summary>
    [BsonElement("wordPhonetic")]
    [BsonIgnoreIfNull]
    public string? WordPhonetic { get; set; }

    /// <summary>
    /// 发音音频文件的URL（可选）
    /// </summary>
    [BsonElement("pronunciationUrl")]
    [BsonIgnoreIfNull]
    public string? PronunciationUrl { get; set; }

    /// <summary>
    /// 练习练习的类型
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public PracticeType Type { get; set; }

    /// <summary>
    /// 此练习任务的预期正确答案
    /// </summary>
    [BsonElement("expectedAnswer")]
    [BsonRequired]
    public string ExpectedAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 练习任务的当前状态
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
    public PracticeTaskStatus Status { get; set; } = PracticeTaskStatus.Pending;

    /// <summary>
    /// 练习任务创建时间
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 练习任务完成时间（如果适用）
    /// </summary>
    [BsonElement("completedAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 可用的练习练习类型
/// </summary>
public enum PracticeType
{
    /// <summary>
    /// 听写练习 - 听发音并写出单词
    /// </summary>
    Dictation = 1,

    /// <summary>
    /// 定义到单词练习 - 阅读定义并写出单词
    /// </summary>
    DefinitionToWord = 2
}

/// <summary>
/// 练习任务的状态
/// </summary>
public enum PracticeTaskStatus
{
    /// <summary>
    /// 任务已创建但尚未开始
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 任务正在进行中
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// 任务已完成
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 任务已开始但被放弃
    /// </summary>
    Abandoned = 4
}