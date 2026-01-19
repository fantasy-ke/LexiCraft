using BuildingBlocks.MongoDB;

namespace LexiCraft.Services.Practice.Assessments.Models;

public class MistakeItem(string userId, string wordId, Guid answerId, MistakeType type, string? input, string correct)
    : MongoEntity
{
    public string UserId { get; private set; } = userId;
    public string WordId { get; private set; } = wordId;
    public Guid AnswerRecordId { get; private set; } = answerId;
    public MistakeType MistakeType { get; private set; } = type;
    public string? UserInput { get; private set; } = input;
    public string CorrectSpelling { get; private set; } = correct;
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
}