using BuildingBlocks.MongoDB;

namespace LexiCraft.Services.Practice.Assessments.Models;

public class MistakeItem : MongoEntity
{
    public string UserId { get; private set; }
    public string WordId { get; private set; }
    public Guid AnswerRecordId { get; private set; }
    public MistakeType MistakeType { get; private set; }
    public string? UserInput { get; private set; }
    public string CorrectSpelling { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private MistakeItem() { }

    public MistakeItem(string userId, string wordId, Guid answerId, MistakeType type, string? input, string correct)
    {
        UserId = userId;
        WordId = wordId;
        AnswerRecordId = answerId;
        MistakeType = type;
        UserInput = input;
        CorrectSpelling = correct;
        OccurredAt = DateTime.UtcNow;
    }
}