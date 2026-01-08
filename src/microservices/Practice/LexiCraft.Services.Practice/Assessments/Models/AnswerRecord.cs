using BuildingBlocks.MongoDB;

namespace LexiCraft.Services.Practice.Assessments.Models;

public class AnswerRecord : MongoEntity
{
    public Guid PracticeTaskItemId { get; private set; }
    public string? UserInput { get; private set; }
    public AnswerStatus Status { get; private set; }
    public int Score { get; private set; }
    public long ResponseTimeMs { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public AssessmentType AssessmentType { get; private set; }

    private AnswerRecord() { }

    public AnswerRecord(Guid itemId, string? input, AnswerStatus status, int score, long responseTime, AssessmentType assessmentType)
    {
        PracticeTaskItemId = itemId;
        UserInput = input;
        Status = status;
        Score = score;
        ResponseTimeMs = responseTime;
        SubmittedAt = DateTime.UtcNow;
        AssessmentType = assessmentType;
    }
}